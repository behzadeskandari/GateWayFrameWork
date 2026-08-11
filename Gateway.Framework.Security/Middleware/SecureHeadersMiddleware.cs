using Gateway.Framework.Logging.Audit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Gateway.Framework.Security.Middleware;

public sealed class SecureHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public SecureHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-XSS-Protection"] = "0";
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none';";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            if (!_environment.IsDevelopment())
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class SecureHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewaySecureHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecureHeadersMiddleware>();
}

public sealed class IpAllowListOptions
{
    public const string SectionName = "Security:IpAllowList";
    public bool Enabled { get; set; }
    public string[] AllowedIps { get; set; } = Array.Empty<string>();
}

public sealed class IpAllowListMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IpAllowListOptions _options;

    public IpAllowListMiddleware(RequestDelegate next, IOptions<IpAllowListOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogger auditLogger)
    {
        if (_options.Enabled && _options.AllowedIps.Length > 0)
        {
            var remoteIp = ResolveClientIp(context);
            if (remoteIp is null || !_options.AllowedIps.Contains(remoteIp))
            {
                context.Items["ip_allow_list_rejected"] = true;
                await auditLogger.LogAsync(
                    AuditActions.IpRejected,
                    context.Request.Path,
                    AuditOutcomes.Denied,
                    AuditContext.FromHttpContext(context));

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden: IP address not allowed.");
                return;
            }
        }

        await _next(context);
    }

    public static string? ResolveClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();
}

public static class IpAllowListMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewayIpAllowList(this IApplicationBuilder app) =>
        app.UseMiddleware<IpAllowListMiddleware>();

    public static IServiceCollection AddGatewayIpAllowList(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<IpAllowListOptions>(configuration.GetSection(IpAllowListOptions.SectionName));
        return services;
    }
}

public sealed class RequestSizeLimitOptions
{
    public const string SectionName = "Security:RequestSizeLimit";
    public long MaxRequestBodySizeBytes { get; set; } = 1_048_576;
}

public sealed class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestSizeLimitOptions _options;

    public RequestSizeLimitMiddleware(RequestDelegate next, IOptions<RequestSizeLimitOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > _options.MaxRequestBodySizeBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsync("Request body exceeds allowed size.");
            return;
        }

        context.Request.Body = new SizeLimitedStream(context.Request.Body, _options.MaxRequestBodySizeBytes);
        await _next(context);
    }

    private sealed class SizeLimitedStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _maxBytes;
        private long _readBytes;

        public SizeLimitedStream(Stream inner, long maxBytes)
        {
            _inner = inner;
            _maxBytes = maxBytes;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            _readBytes += read;
            if (_readBytes > _maxBytes)
            {
                throw new BadHttpRequestException("Request body exceeds allowed size.");
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            var read = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            _readBytes += read;
            if (_readBytes > _maxBytes)
            {
                throw new BadHttpRequestException("Request body exceeds allowed size.");
            }

            return read;
        }
    }
}

public static class RequestSizeLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewayRequestSizeLimit(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestSizeLimitMiddleware>();

    public static IServiceCollection AddGatewayRequestSizeLimit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RequestSizeLimitOptions>(configuration.GetSection(RequestSizeLimitOptions.SectionName));
        return services;
    }
}
