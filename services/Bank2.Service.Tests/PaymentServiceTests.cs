using Bank2.Service.Application.Models;
using Bank2.Service.Application.Services;
using Bank2.Service.Infrastructure.Data;
using Xunit;

namespace Bank2.Service.Tests;

public sealed class PaymentServiceTests
{
    [Fact]
    public async Task CreatePayment_WithSameIdempotencyKey_ReturnsSameResult()
    {
        var service = new PaymentService(new InMemoryPaymentRepository());
        var request = new CreatePaymentRequest("acc-1001", "acc-1002", 15m, "USD", "demo");
        var first = await service.CreatePaymentAsync(request, "key-abc");
        var second = await service.CreatePaymentAsync(request, "key-abc");
        Assert.Equal(first.Id, second.Id);
    }
}
