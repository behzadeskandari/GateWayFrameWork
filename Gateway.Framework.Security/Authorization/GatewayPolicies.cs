namespace Gateway.Framework.Security.Authorization;

public static class GatewayPolicies
{
    public const string AuthenticatedUser = "AuthenticatedUser";
    public const string BankingOperator = "BankingOperator";
    public const string BankingAdmin = "BankingAdmin";
    public const string RequiredScopes = "RequiredScopes";
}
