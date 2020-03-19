using System;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Wrappers;
using Vostok.Logging.Formatting;
using VostokLogLevel = Vostok.Logging.Abstractions.LogLevel;
using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Vostok.Logging.Microsoft
{
    [PublicAPI]
    public sealed class MicrosoftLog : ILog
    {
        private const int NullEventId = 0;
        private static readonly Func<FormattedLogValues, Exception, string> MessageFormatter = FormatMessage;
        private static readonly OutputTemplate Template = OutputTemplate.Parse(
            $"{{{WellKnownProperties.TraceContext}:w}}" +
            $"{{{WellKnownProperties.OperationContext}:w}}" +
            $"{{{WellKnownProperties.SourceContext}:w}}" +
            $"{{{WellKnownTokens.Message}}}");

        private readonly ILogger logger;
        private readonly MicrosoftLogSettings settings;

        public MicrosoftLog([NotNull] ILogger logger, [CanBeNull] MicrosoftLogSettings settings = null)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = settings ?? new MicrosoftLogSettings();
        }

        public void Log(LogEvent @event)
        {
            if (@event is null)
                return;

            if (!IsEnabledFor(@event.Level))
                return;

            var logLevel = ConvertLogLevel(@event.Level);

            var message = settings.UseVostokTemplate
                ? LogEventFormatter.Format(@event, Template)
                : LogMessageFormatter.Format(@event);

            var state = new FormattedLogValues(message, Array.Empty<object>());

            logger.Log(logLevel, NullEventId, state, @event.Exception, MessageFormatter);
        }

        private static string FormatMessage(FormattedLogValues state, Exception error)
        {
            return state.ToString();
        }

        public bool IsEnabledFor(VostokLogLevel level)
        {
            var logLevel = ConvertLogLevel(level);
            return logger.IsEnabled(logLevel);
        }

        public ILog ForContext(string context)
        {
            return new SourceContextWrapper(this, context);
        }

        private static MicrosoftLogLevel ConvertLogLevel(VostokLogLevel logLevel)
        {
            switch (logLevel)
            {
                case VostokLogLevel.Debug:
                    return MicrosoftLogLevel.Debug;
                case VostokLogLevel.Info:
                    return MicrosoftLogLevel.Information;
                case VostokLogLevel.Warn:
                    return MicrosoftLogLevel.Warning;
                case VostokLogLevel.Error:
                    return MicrosoftLogLevel.Error;
                case VostokLogLevel.Fatal:
                    return MicrosoftLogLevel.Critical;
                default:
                    return MicrosoftLogLevel.None;
            }
        }
    }
}