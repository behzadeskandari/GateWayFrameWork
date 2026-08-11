using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bank2.Service.Application.Services;

public static class IdempotencyFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string Compute<T>(T request)
    {
        var payload = JsonSerializer.Serialize(request, SerializerOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
