namespace Gateway.Framework.Plugins.Abstractions;

[Flags]
public enum BankingPluginCapability
{
    None = 0,
    Accounts = 1 << 0,
    Balance = 1 << 1,
    Payment = 1 << 2,
    Transfer = 1 << 3,
    Cheque = 1 << 4,
    Card = 1 << 5,
    Statement = 1 << 6,
    Customer = 1 << 7,
    Transaction = 1 << 8
}
