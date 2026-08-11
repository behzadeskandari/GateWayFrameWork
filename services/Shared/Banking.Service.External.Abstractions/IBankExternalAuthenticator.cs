namespace Banking.Service.External.Abstractions;

public interface IBankExternalAuthenticator
{
    Task ApplyAuthenticationAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}
