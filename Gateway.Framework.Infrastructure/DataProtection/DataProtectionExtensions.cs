using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Framework.Infrastructure.DataProtection;

public interface IEncryptionService
{
    string Protect(string plaintext);
    string Unprotect(string protectedText);
}

public sealed class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    public EncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Gateway.Framework.SensitiveData");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}

public static class DataProtectionExtensions
{
    public static IServiceCollection AddGatewayDataProtection(this IServiceCollection services)
    {
        services.AddDataProtection()
            .SetApplicationName("Gateway.Framework");

        services.AddSingleton<IEncryptionService, EncryptionService>();
        return services;
    }

    public static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
