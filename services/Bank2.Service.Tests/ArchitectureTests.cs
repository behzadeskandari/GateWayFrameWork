using Xunit;

namespace Bank2.Service.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_DoesNotReferenceInfrastructureOrAspNetCore()
    {
        var domainAssembly = typeof(Bank2.Service.Domain.Entities.Payment).Assembly;
        var references = domainAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        Assert.DoesNotContain("Bank2.Service.Infrastructure", references);
        Assert.DoesNotContain("Microsoft.AspNetCore", references, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructureOrApi()
    {
        var applicationAssembly = typeof(Bank2.Service.Application.Features.Payments.GetPayments.GetPaymentsHandler).Assembly;
        var references = applicationAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        Assert.DoesNotContain("Bank2.Service.Infrastructure", references);
        Assert.DoesNotContain("Bank2.Service", references);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bank2_DoesNotReferenceBank1()
    {
        var bank2Assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Bank2.Service", StringComparison.Ordinal) == true)
            .ToList();

        foreach (var assembly in bank2Assemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(a => a.Name);
            Assert.DoesNotContain(references, name => name?.StartsWith("Bank1.Service", StringComparison.Ordinal) == true);
        }
    }
}
