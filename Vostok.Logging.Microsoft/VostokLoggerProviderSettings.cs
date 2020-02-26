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
    }
}