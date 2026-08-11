using Bank2.Service.Application.Features.Transfers.CreateTransfer;
using Bank2.Service.Application.Features.Transfers.GetTransferStatus;
using Bank2.Service.Application.Features.Transfers.GetTransfers;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Bank2.Service.Controllers;

[ApiController]
[Route("api/transfers")]
public sealed class TransfersController : ControllerBase
{
    private readonly IGetTransfersHandler _getTransfersHandler;
    private readonly ICreateTransferHandler _createTransferHandler;
    private readonly IGetTransferStatusHandler _getTransferStatusHandler;

    public TransfersController(
        IGetTransfersHandler getTransfersHandler,
        ICreateTransferHandler createTransferHandler,
        IGetTransferStatusHandler getTransferStatusHandler)
    {
        _getTransfersHandler = getTransfersHandler;
        _createTransferHandler = createTransferHandler;
        _getTransferStatusHandler = getTransferStatusHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TransfersListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
        var response = await _getTransfersHandler.HandleAsync(correlationId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(string id, CancellationToken cancellationToken)
    {
        var result = await _getTransferStatusHandler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Create([FromBody] CreateTransferRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = HttpContext.Items[IdempotencyMiddleware.IdempotencyKeyItemKey] as string;
        var correlationId = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
        var result = await _createTransferHandler.HandleAsync(request, idempotencyKey, correlationId, cancellationToken);
        return Accepted(new
        {
            Service = "Bank2.Service",
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey,
            Data = result
        });
    }
}
