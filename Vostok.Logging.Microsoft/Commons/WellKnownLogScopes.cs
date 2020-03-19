using JetBrains.Annotations;

namespace Vostok.Logging.Microsoft.Commons
{
    [PublicAPI]
    public static class WellKnownLogScopes
    {
        public const string ActionLogScope = "Microsoft.AspNetCore.Mvc.MvcCoreLoggerExtensions+ActionLogScope";
        public const string HostingLogScope = "Microsoft.AspNetCore.Hosting.HostingLoggerExtensions+HostingLogScope";
        public const string ConnectionLogScope = "Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ConnectionLogScope";
        public const string HealthCheckLogScope = "Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckLogScope";
        public const string ViewComponentLogScope = "Microsoft.AspNetCore.Mvc.ViewFeatures.MvcViewFeaturesLoggerExtensions+ViewComponentLogScope";
    }
}