using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Vostok.Logging.Abstractions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Vostok.Logging.Microsoft
{
    /// <inheritdoc />
    /// <summary>
    /// <para>Logger provider for writing log events into Vostok <see cref="ILog"/></para>
    /// </summary>
    public class VostokLoggerProvider : ILoggerProvider
    {
        private readonly ILog log;

        /// <summary>
        /// <para>Create a new <see cref="VostokLoggerProvider"/> for given root <paramref name="log"/></para>
        /// </summary>
        /// <param name="log"><see cref="ILog"/> to write log events to</param>
        public VostokLoggerProvider([NotNull] ILog log)
        {
            this.log = log;
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
        /// <summary>
        /// </summary>
        public void Dispose()
        {
        }

        private class Logger : ILogger
        {
            private const string OriginalFormatKey = "{OriginalFormat}";

            private ILog log;

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
                var logEvent = EnrichWithProperties(new LogEvent(translatedLevel, DateTimeOffset.UtcNow, messageTemplate, exception), state);
                log.Log(logEvent);
            }

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && log.IsEnabledFor(TranslateLogLevel(logLevel));

            public IDisposable BeginScope<TState>(TState state)
            {
                return new Scope<TState>(this, state);
            }

            private static LogEvent EnrichWithProperties<TState>(LogEvent logEvent, TState state)
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

            private class Scope<TState> : IDisposable
            {
                private readonly Logger owner;
                private readonly ILog prevLog;
                private bool disposed;

                public Scope(Logger owner, TState state)
                {
                    this.owner = owner;
                    prevLog = owner.log;
                    owner.log = prevLog.WithObjectProperties(state);
                }

                public void Dispose()
                {
                    if (disposed)
                        return;
                    owner.log = prevLog;
                    disposed = true;
                }
            }
        }
    }
}