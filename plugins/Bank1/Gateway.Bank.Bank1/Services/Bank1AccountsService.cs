namespace Gateway.Bank.Bank1;

public interface IBank1AccountsService
{
    Task<string> GetSampleAccountStatusAsync(CancellationToken cancellationToken = default);
}

public sealed class Bank1AccountsService : IBank1AccountsService
{
    private readonly Bank1AccountsClient _client;

    public Bank1AccountsService(Bank1AccountsClient client) => _client = client;

    public async Task<string> GetSampleAccountStatusAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _client.Client.GetAsync("health", cancellationToken);
        return response.IsSuccessStatusCode ? "Sample downstream reachable" : "Sample downstream unavailable";
    }
}

public sealed class Bank1AccountsClient
{
    public Bank1AccountsClient(HttpClient client) => Client = client;

    public HttpClient Client { get; }
}
