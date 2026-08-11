using System.Net;
using System.Text.Json;
using Banking.Service.External.Abstractions;

namespace Banking.Service.External.Abstractions.Http;

public static class ExternalBankHttpResponseHandler
{
    public static async Task EnsureSuccessOrThrowAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var statusCode = (int)response.StatusCode;
        var body = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        var errorCode = TryReadErrorCode(body);

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new ExternalBankAuthenticationException("External bank rejected the request credentials."),
            HttpStatusCode.NotFound =>
                new ExternalBankResponseException("External bank resource was not found.", statusCode, errorCode),
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout =>
                new ExternalBankTimeoutException($"External bank request timed out with status {statusCode}."),
            HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable =>
                new ExternalBankUnavailableException($"External bank is unavailable (status {statusCode}).", statusCode),
            _ => new ExternalBankResponseException(
                $"External bank returned status {statusCode}.",
                statusCode,
                errorCode)
        };
    }

    private static string? TryReadErrorCode(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("errorCode", out var errorCode))
            {
                return errorCode.GetString();
            }

            if (document.RootElement.TryGetProperty("code", out var code))
            {
                return code.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
