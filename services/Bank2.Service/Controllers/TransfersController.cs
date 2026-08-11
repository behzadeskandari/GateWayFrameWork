using Bank2.Service.Application.Features.Transfers.CreateTransfer;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Bank2.Service.Controllers;

[ApiController]
[Route("api/transfers")]
public sealed class TransfersController : ControllerBase
{
    private readonly ICreateTransferHandler _createTransferHandler;

    public TransfersController(ICreateTransferHandler createTransferHandler) =>
        _createTransferHandler = createTransferHandler;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Create([FromBody] CreateTransferRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = HttpContext.Items[IdempotencyMiddleware.IdempotencyKeyItemKey] as string;
        var correlationId = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
        var result = await _createTransferHandler.HandleAsync(request, idempotencyKey, cancellationToken);
        return Accepted(new
        {
            Service = "Bank2.Service",
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey,
            Data = result
        });
    }
}
