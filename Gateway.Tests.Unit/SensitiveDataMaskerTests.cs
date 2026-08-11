using Gateway.Framework.Logging.Serilog.Masking;
using Xunit;

namespace Gateway.Tests.Unit;

public sealed class SensitiveDataMaskerLegacyTests
{
    [Fact]
    public void Mask_RedactsJwtTokens()
    {
        var options = new LogMaskingOptions();
        var input = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIn0.signature";
        var masked = SensitiveDataMasker.Mask(input, options);
        Assert.Contains("***REDACTED***", masked);
        Assert.DoesNotContain("eyJhbGci", masked);
    }

    [Fact]
    public void Mask_ReturnsEmptyWhenInputIsNull()
    {
        var options = new LogMaskingOptions();
        Assert.Equal(string.Empty, SensitiveDataMasker.Mask(null, options));
    }
}
