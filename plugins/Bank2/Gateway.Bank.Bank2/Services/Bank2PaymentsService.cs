namespace Gateway.Bank.Bank2;

public interface IBank2PaymentsService
{
    Task<string> GetSamplePaymentStatusAsync(CancellationToken cancellationToken = default);
}

public sealed class Bank2PaymentsService : IBank2PaymentsService
{
    private readonly Bank2PaymentsClient _client;

    public Bank2PaymentsService(Bank2PaymentsClient client) => _client = client;

    public async Task<string> GetSamplePaymentStatusAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _client.Client.GetAsync("health", cancellationToken);
        return response.IsSuccessStatusCode ? "Sample downstream reachable" : "Sample downstream unavailable";
    }
}

public sealed class Bank2PaymentsClient
{
    public Bank2PaymentsClient(HttpClient client) => Client = client;

    public HttpClient Client { get; }
}
