using Xunit;

namespace Bank1.Service.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_DoesNotReferenceInfrastructureOrAspNetCore()
    {
        var domainAssembly = typeof(Bank1.Service.Domain.Entities.Account).Assembly;
        var references = domainAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        Assert.DoesNotContain("Bank1.Service.Infrastructure", references);
        Assert.DoesNotContain("Microsoft.AspNetCore", references, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructureOrApi()
    {
        var applicationAssembly = typeof(Bank1.Service.Application.Features.Accounts.GetAccounts.GetAccountsHandler).Assembly;
        var references = applicationAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        Assert.DoesNotContain("Bank1.Service.Infrastructure", references);
        Assert.DoesNotContain("Bank1.Service", references);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Contracts_DoNotReferenceInfrastructure()
    {
        var contractsAssembly = typeof(Bank1.Service.Contracts.Accounts.AccountSummaryResponse).Assembly;
        var references = contractsAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        Assert.DoesNotContain("Bank1.Service.Infrastructure", references);
        Assert.DoesNotContain("Bank2.Service", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bank1_DoesNotReferenceBank2()
    {
        var bank1Assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Bank1.Service", StringComparison.Ordinal) == true)
            .ToList();

        foreach (var assembly in bank1Assemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(a => a.Name);
            Assert.DoesNotContain(references, name => name?.StartsWith("Bank2.Service", StringComparison.Ordinal) == true);
        }
    }
}
