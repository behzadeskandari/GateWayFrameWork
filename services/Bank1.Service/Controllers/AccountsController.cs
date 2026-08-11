using Bank1.Service.Application.Features.Accounts.GetAccount;
using Bank1.Service.Application.Features.Accounts.GetAccounts;
using Bank1.Service.Application.Features.Accounts.GetBalance;
using Bank1.Service.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Bank1.Service.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IGetAccountsHandler _getAccountsHandler;
    private readonly IGetAccountHandler _getAccountHandler;
    private readonly IGetBalanceHandler _getBalanceHandler;

    public AccountsController(
        IGetAccountsHandler getAccountsHandler,
        IGetAccountHandler getAccountHandler,
        IGetBalanceHandler getBalanceHandler)
    {
        _getAccountsHandler = getAccountsHandler;
        _getAccountHandler = getAccountHandler;
        _getBalanceHandler = getBalanceHandler;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
        var response = await _getAccountsHandler.HandleAsync(correlationId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var account = await _getAccountHandler.HandleAsync(id, cancellationToken);
        if (account is null)
        {
            return NotFound(new { Message = "Account not found.", Id = id });
        }

        var correlationId = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
        return Ok(new
        {
            Service = "Bank1.Service",
            CorrelationId = correlationId,
            Data = account
        });
    }

    [HttpGet("{id}/balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(string id, CancellationToken cancellationToken)
    {
        var balance = await _getBalanceHandler.HandleAsync(id, cancellationToken);
        if (balance is null)
        {
            return NotFound(new { Message = "Account not found.", Id = id });
        }

        var correlationId = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
        return Ok(new
        {
            Service = "Bank1.Service",
            CorrelationId = correlationId,
            Data = balance
        });
    }
}
