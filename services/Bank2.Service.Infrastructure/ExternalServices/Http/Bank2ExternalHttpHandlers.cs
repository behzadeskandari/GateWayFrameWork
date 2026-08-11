using Banking.Service.External.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Bank2.Service.Infrastructure.ExternalServices.Http;

internal sealed class HttpCorrelationContext : ICorrelationContext
{
    private const string CorrelationIdItemKey = "CorrelationId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCorrelationContext(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Items[CorrelationIdItemKey] as string;
}

internal sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string CorrelationHeaderName = "X-Correlation-Id";
    private readonly ICorrelationContext _correlationContext;

    public CorrelationIdDelegatingHandler(ICorrelationContext correlationContext) =>
        _correlationContext = correlationContext;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_correlationContext.CorrelationId))
        {
            request.Headers.Remove(CorrelationHeaderName);
            request.Headers.TryAddWithoutValidation(CorrelationHeaderName, _correlationContext.CorrelationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

internal sealed class BankExternalAuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IBankExternalAuthenticator _authenticator;

    public BankExternalAuthenticationDelegatingHandler(IBankExternalAuthenticator authenticator) =>
        _authenticator = authenticator;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await _authenticator.ApplyAuthenticationAsync(request, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
