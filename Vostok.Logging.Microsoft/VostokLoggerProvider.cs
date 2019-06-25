using System;
using System.Collections.Generic;
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

        /// <summary>
        /// <para>Create a new <see cref="VostokLoggerProvider"/> for given root <paramref name="log"/></para>
        /// </summary>
        /// <param name="log"><see cref="ILog"/> to write log events to</param>
        public VostokLoggerProvider([NotNull] ILog log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
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
            return new Logger(string.IsNullOrEmpty(categoryName) ? log : log.ForContext(categoryName));
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        private class Logger : ILogger
        {
            private const string OriginalFormatKey = "{OriginalFormat}";

            private readonly ILog log;
            private readonly AsyncLocal<Scope> currentScope = new AsyncLocal<Scope>();

            public Logger(ILog log)
            {
                this.log = log;
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
                (currentScope.Value?.Log ?? log).Log(logEvent);
            }

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && log.IsEnabledFor(TranslateLogLevel(logLevel));

            public IDisposable BeginScope<TState>(TState state)
            {
                var scopeValue = ReferenceEquals(state, null) ? typeof(TState).FullName : Convert.ToString(state);

                var properties = new Dictionary<string, object>();
                if (state is IEnumerable<KeyValuePair<string, object>> props)
                {
                    foreach (var kvp in props)
                    {
                        if (kvp.Key != OriginalFormatKey)
                            properties[kvp.Key] = kvp.Value;
                    }
                }
                
                var scopeLog = log.WithProperties(properties).WithOperationContext();
                
                var scope = new Scope(scopeLog, scopeValue);
                currentScope.Value = scope;
                return scope;
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

            private class Scope : IDisposable
            {
                private readonly OperationContextToken operationContextToken;

                public readonly ILog Log;

                public Scope(ILog log, string scopeValue)
                {
                    Log = log;
                    operationContextToken = new OperationContextToken(scopeValue);
                }

                public void Dispose()
                {
                    operationContextToken.Dispose();
                }
            }
        }
    }
}