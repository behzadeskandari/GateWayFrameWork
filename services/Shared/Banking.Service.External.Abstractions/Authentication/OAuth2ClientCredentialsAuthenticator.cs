using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Banking.Service.External.Abstractions.Authentication;

public sealed class OAuth2ClientCredentialsAuthenticator : IBankExternalAuthenticator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _tokenEndpoint;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string? _scope;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OAuth2ClientCredentialsAuthenticator> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public OAuth2ClientCredentialsAuthenticator(
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string? scope,
        HttpClient httpClient,
        ILogger<OAuth2ClientCredentialsAuthenticator> logger)
    {
        _tokenEndpoint = tokenEndpoint;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _scope = scope;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task ApplyAuthenticationAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt.AddMinutes(-1))
            {
                return _cachedToken;
            }

            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(BuildTokenRequestBody())
            };

            using var response = await _httpClient.SendAsync(tokenRequest, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new ExternalBankAuthenticationException("External bank token endpoint rejected client credentials.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalBankUnavailableException(
                    $"External bank token endpoint returned {(int)response.StatusCode}.",
                    (int)response.StatusCode);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<OAuthTokenResponse>(stream, JsonOptions, cancellationToken)
                ?? throw new ExternalBankResponseException("Token endpoint returned an empty response.", (int)response.StatusCode);

            if (string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                throw new ExternalBankResponseException("Token endpoint did not return an access token.", (int)response.StatusCode);
            }

            _cachedToken = payload.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn <= 0 ? 300 : payload.ExpiresIn);
            _logger.LogDebug("Obtained external bank access token expiring at {ExpiresAt}.", _expiresAt);
            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private IEnumerable<KeyValuePair<string, string>> BuildTokenRequestBody()
    {
        yield return new KeyValuePair<string, string>("grant_type", "client_credentials");
        yield return new KeyValuePair<string, string>("client_id", _clientId);
        yield return new KeyValuePair<string, string>("client_secret", _clientSecret);
        if (!string.IsNullOrWhiteSpace(_scope))
        {
            yield return new KeyValuePair<string, string>("scope", _scope);
        }
    }

    private sealed class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
