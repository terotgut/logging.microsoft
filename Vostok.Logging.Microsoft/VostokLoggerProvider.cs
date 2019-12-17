using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Context;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Vostok.Logging.Microsoft
{
    /// <inheritdoc />
    /// <summary>
    /// <para>Logger provider for writing log events into Vostok <see cref="ILog"/>.</para>
    /// </summary>
    [PublicAPI]
    public class VostokLoggerProvider : ILoggerProvider
    {
        private readonly ILog log;
        private readonly VostokLoggerProviderSettings settings;

        /// <summary>
        /// <para>Create a new <see cref="VostokLoggerProvider"/> for given root <paramref name="log"/>.</para>
        /// </summary>
        /// <param name="log"><see cref="ILog"/> to write log events to.</param>
        public VostokLoggerProvider([NotNull] ILog log, [CanBeNull] VostokLoggerProviderSettings settings = null)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.settings = settings ?? new VostokLoggerProviderSettings();
        }

        /// <inheritdoc />
        /// <summary>
        /// <para>Create a new <see cref="ILogger"/> for given <paramref name="categoryName"/></para> 
        /// <para>
        /// If <paramref name="categoryName"/> is null or empty then returned <see cref="ILogger"/> will write events directly into <see cref="log"/>,
        /// otherwise it will write events into <see cref="log"/>.<see cref="ILog.ForContext"/> with <paramref name="categoryName"/> used as context
        /// </para> 
        /// </summary>
        [NotNull]
        public ILogger CreateLogger([CanBeNull] string categoryName)
        {
            return new Logger(string.IsNullOrEmpty(categoryName) ? log : log.ForContext(categoryName), settings.DisabledScopes);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        private class Logger : ILogger
        {
            private const string OriginalFormatKey = "{OriginalFormat}";

            private readonly ILog log;
            private readonly IReadOnlyCollection<string> disabledScopes;
            private readonly AsyncLocal<UseScope> scope = new AsyncLocal<UseScope>();

            public Logger(ILog log, IReadOnlyCollection<string> disabledScopes)
            {
                this.log = log;
                this.disabledScopes = disabledScopes;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (logLevel == LogLevel.None)
                    return;

                var translatedLevel = TranslateLogLevel(logLevel);
                if (!log.IsEnabledFor(translatedLevel))
                    return;

                var messageTemplate = ExtractMessageTemplate(state, exception, formatter);
                var logEvent = EnrichWithProperties(new LogEvent(translatedLevel, DateTimeOffset.Now, messageTemplate, exception), eventId, state);
                (scope.Value?.Log ?? log).Log(logEvent);
            }

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && log.IsEnabledFor(TranslateLogLevel(logLevel));

            public IDisposable BeginScope<TState>(TState state)
            {
                var scopeName = typeof(TState).FullName;

                if (disabledScopes?.Contains(scopeName) == true)
                    return new EmptyDisposable();

                Console.WriteLine(typeof(TState).FullName);
                var scopeValue = state == null ? scopeName : Convert.ToString(state);
                var scopeLog = log.WithOperationContext();

                if (state is IEnumerable<KeyValuePair<string, object>> props)
                {
                    foreach (var kvp in props)
                    {
                        if (kvp.Key != OriginalFormatKey)
                        {
                            scopeLog = scopeLog.WithProperty(kvp.Key, kvp.Value);
                        }
                    }
                }
                
                return new UseScope(scopeLog, scopeValue, scope);
            }

            private static LogEvent EnrichWithProperties<TState>(LogEvent logEvent, EventId eventId, TState state)
            {
                if (state is IEnumerable<KeyValuePair<string, object>> props)
                {
                    foreach (var kvp in props)
                    {
                        if (kvp.Key == OriginalFormatKey)
                            continue;
                        logEvent = logEvent.WithPropertyIfAbsent(kvp.Key, kvp.Value);
                    }
                }

                if (eventId.Id != 0)
                    logEvent = logEvent.WithPropertyIfAbsent("EventId.Id", eventId.Id);
                if (!string.IsNullOrEmpty(eventId.Name))
                    logEvent = logEvent.WithPropertyIfAbsent("EventId.Name", eventId.Name);

                return logEvent;
            }

            private static string ExtractMessageTemplate<TState>(TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (state is IEnumerable<KeyValuePair<string, object>> props)
                {
                    foreach (var kvp in props)
                    {
                        if (kvp.Key == OriginalFormatKey)
                            return Convert.ToString(kvp.Value);
                    }
                }

                if (formatter != null)
                    return formatter(state, exception);

                return ReferenceEquals(state, null) ? typeof(TState).FullName : Convert.ToString(state);
            }

            private Abstractions.LogLevel TranslateLogLevel(LogLevel logLevel)
            {
                switch (logLevel)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                        return Abstractions.LogLevel.Debug;
                    case LogLevel.Information:
                        return Abstractions.LogLevel.Info;
                    case LogLevel.Warning:
                        return Abstractions.LogLevel.Warn;
                    case LogLevel.Error:
                        return Abstractions.LogLevel.Error;
                    case LogLevel.Critical:
                        return Abstractions.LogLevel.Fatal;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
                }
            }

            private class UseScope : IDisposable
            {
                public readonly ILog Log;
                
                private readonly OperationContextToken operationContextToken;
                private readonly AsyncLocal<UseScope> scope;
                private readonly UseScope previousScopeValue;

                public UseScope(ILog log, string scopeValue, AsyncLocal<UseScope> scope)
                {
                    Log = log;
                    this.scope = scope;
                    operationContextToken = new OperationContextToken(scopeValue);
                    previousScopeValue = scope.Value;
                    scope.Value = this;
                }

                public void Dispose()
                {
                    operationContextToken.Dispose();
                    scope.Value = previousScopeValue;
                }
            }

            private class EmptyDisposable : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}