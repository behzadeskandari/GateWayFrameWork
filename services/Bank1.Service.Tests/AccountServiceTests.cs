using Bank1.Service.Application.Services;
using Bank1.Service.Infrastructure.Data;
using Xunit;

namespace Bank1.Service.Tests;

public sealed class AccountServiceTests
{
    [Fact]
    public async Task GetBalance_ForUnknownAccount_ReturnsNull()
    {
        var service = new AccountService(new InMemoryAccountRepository());
        var balance = await service.GetBalanceAsync("missing");
        Assert.Null(balance);
    }

    [Fact]
    public async Task ListAccounts_ReturnsSampleAccounts()
    {
        var service = new AccountService(new InMemoryAccountRepository());
        var accounts = await service.ListAccountsAsync();
        Assert.True(accounts.Count >= 2);
    }
}
