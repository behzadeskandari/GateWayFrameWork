namespace Bank2.Service.Application.Configuration;

public sealed class Bank2ProxyOptions
{
    public const string SectionName = "Bank2Proxy";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://localhost:5102/";
}
