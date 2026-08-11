using Banking.Service.External.Abstractions;

namespace Banking.Service.External.Abstractions.Authentication;

public sealed class NoneBankExternalAuthenticator : IBankExternalAuthenticator
{
    public Task ApplyAuthenticationAsync(HttpRequestMessage request, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public sealed class ApiKeyBankExternalAuthenticator : IBankExternalAuthenticator
{
    private readonly string _headerName;
    private readonly string _apiKey;

    public ApiKeyBankExternalAuthenticator(string headerName, string apiKey)
    {
        _headerName = headerName;
        _apiKey = apiKey;
    }

    public Task ApplyAuthenticationAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        request.Headers.Remove(_headerName);
        request.Headers.TryAddWithoutValidation(_headerName, _apiKey);
        return Task.CompletedTask;
    }
}
