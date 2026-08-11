using Bank1.Service.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bank1.Service.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService) => _accountService = accountService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var accounts = await _accountService.ListAccountsAsync(cancellationToken);
        return Ok(new
        {
            Service = "Bank1.Service",
            CorrelationId = HttpContext.Items["CorrelationId"],
            Data = accounts
        });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var account = await _accountService.GetAccountAsync(id, cancellationToken);
        if (account is null)
        {
            return NotFound(new { Message = "Account not found.", Id = id });
        }

        return Ok(new
        {
            Service = "Bank1.Service",
            CorrelationId = HttpContext.Items["CorrelationId"],
            Data = account
        });
    }

    [HttpGet("{id}/balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(string id, CancellationToken cancellationToken)
    {
        var balance = await _accountService.GetBalanceAsync(id, cancellationToken);
        if (balance is null)
        {
            return NotFound(new { Message = "Account not found.", Id = id });
        }

        return Ok(new
        {
            Service = "Bank1.Service",
            CorrelationId = HttpContext.Items["CorrelationId"],
            Data = balance
        });
    }
}
