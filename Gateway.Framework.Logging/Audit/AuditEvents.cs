namespace Gateway.Framework.Logging.Audit;

public static class AuditActions
{
    public const string AuthenticationSuccess = "authentication.success";
    public const string AuthenticationFailure = "authentication.failure";
    public const string AuthorizationFailure = "authorization.failure";
    public const string RateLimitRejected = "rate_limit.rejected";
    public const string IpRejected = "ip_allow_list.rejected";
    public const string ProtectedRouteAccess = "route.protected_access";
    public const string ConfigurationFailure = "configuration.failure";
    public const string PluginInitializationFailure = "plugin.initialization_failure";
}

public static class AuditOutcomes
{
    public const string Success = "success";
    public const string Failure = "failure";
    public const string Denied = "denied";
}
