using Bank2.Service.Application.Models;
using Bank2.Service.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bank2.Service.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService) => _paymentService = paymentService;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var payments = await _paymentService.ListPaymentsAsync(cancellationToken);
        return Ok(new
        {
            Service = "Bank2.Service",
            CorrelationId = HttpContext.Items["CorrelationId"],
            Data = payments
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = HttpContext.Items["IdempotencyKey"]?.ToString();
        var result = await _paymentService.CreatePaymentAsync(request, idempotencyKey, cancellationToken);
        return Accepted(new
        {
            Service = "Bank2.Service",
            CorrelationId = HttpContext.Items["CorrelationId"],
            IdempotencyKey = idempotencyKey,
            Data = result
        });
    }
}

[ApiController]
[Route("api/transfers")]
public sealed class TransfersController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public TransfersController(IPaymentService paymentService) => _paymentService = paymentService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransferRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = HttpContext.Items["IdempotencyKey"]?.ToString();
        var result = await _paymentService.CreateTransferAsync(request, idempotencyKey, cancellationToken);
        return Accepted(new
        {
            Service = "Bank2.Service",
            CorrelationId = HttpContext.Items["CorrelationId"],
            IdempotencyKey = idempotencyKey,
            Data = result
        });
    }
}
