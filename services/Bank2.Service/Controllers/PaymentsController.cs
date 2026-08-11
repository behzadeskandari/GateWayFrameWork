using Bank2.Service.Application.Features.Payments.CreatePayment;
using Bank2.Service.Application.Features.Payments.GetPayments;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Bank2.Service.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IGetPaymentsHandler _getPaymentsHandler;
    private readonly ICreatePaymentHandler _createPaymentHandler;

    public PaymentsController(
        IGetPaymentsHandler getPaymentsHandler,
        ICreatePaymentHandler createPaymentHandler)
    {
        _getPaymentsHandler = getPaymentsHandler;
        _createPaymentHandler = createPaymentHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaymentsListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
        var response = await _getPaymentsHandler.HandleAsync(correlationId, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = HttpContext.Items[IdempotencyMiddleware.IdempotencyKeyItemKey] as string;
        var correlationId = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
        var result = await _createPaymentHandler.HandleAsync(request, idempotencyKey, cancellationToken);
        return Accepted(new
        {
            Service = "Bank2.Service",
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey,
            Data = result
        });
    }
}
