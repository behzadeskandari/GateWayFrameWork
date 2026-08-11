using Bank1.Service.Domain.Exceptions;

namespace Bank1.Service.Domain.Rules;

public static class AccountRules
{
    public static void EnsureNonNegativeBalance(decimal balance)
    {
        if (balance < 0)
        {
            throw new DomainException("Account balance cannot be negative.");
        }
    }

    public static void EnsureActiveForBalanceInquiry(Enums.AccountStatus status)
    {
        if (status == Enums.AccountStatus.Closed)
        {
            throw new DomainException("Balance cannot be retrieved for a closed account.");
        }
    }
}
