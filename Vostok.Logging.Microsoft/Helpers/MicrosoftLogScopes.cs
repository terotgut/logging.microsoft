using JetBrains.Annotations;

namespace Vostok.Logging.Microsoft.Helpers
{
    [PublicAPI]
    public static class MicrosoftLogScopes
    {
        public const string ActionLogScope = "Microsoft.AspNetCore.Mvc.MvcCoreLoggerExtensions+ActionLogScope";
        public const string ActionLogScopeOld = "Microsoft.AspNetCore.Mvc.Internal.MvcCoreLoggerExtensions+ActionLogScope";

        public const string HostingLogScope = "Microsoft.AspNetCore.Hosting.HostingLoggerExtensions+HostingLogScope";
        public const string HostingLogScopeOld = "Microsoft.AspNetCore.Hosting.Internal.HostingLoggerExtensions+HostingLogScope";

        public const string ConnectionLogScope = "Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ConnectionLogScope";

        public const string HealthCheckLogScope = "Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckLogScope";

        public const string ViewComponentLogScope = "Microsoft.AspNetCore.Mvc.ViewFeatures.MvcViewFeaturesLoggerExtensions+ViewComponentLogScope";
    }
}