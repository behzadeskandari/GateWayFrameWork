using System.Diagnostics;

namespace Gateway.Framework.Core.Observability;

public static class ActivitySources
{
    public const string GatewayName = "Gateway.Framework";
    public static readonly ActivitySource Gateway = new(GatewayName);
}
