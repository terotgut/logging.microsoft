using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Vostok.Logging.Microsoft
{
    [PublicAPI]
    public class VostokLoggerProviderSettings
    {
        /// <summary>
        /// Collection of types that will be ignored in <see cref="ILogger.BeginScope{TState}"></see>./>
        /// </summary>
        [CanBeNull]
        public IReadOnlyCollection<Type> DisabledScopes { get; set; }
    }
}