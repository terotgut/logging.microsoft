using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Vostok.Logging.Microsoft
{
    [PublicAPI]
    public class VostokLoggerProviderSettings
    {
        /// <summary>
        /// Full names of types, that should be ignored in <see cref="ILogger.BeginScope{TState}"></see>./>
        /// </summary>
        [CanBeNull]
        public IReadOnlyCollection<string> IgnoredScopes { get; set; }

        /// <summary>
        /// If set to some value, limits the minimum level of log entries.
        /// </summary>
        [CanBeNull]
        public Abstractions.LogLevel? MinimumLevel { get; set; }
    }
}