namespace Bank1.Service.Application.Configuration;

public sealed class Bank1ProxyOptions
{
    public const string SectionName = "Bank1Proxy";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://localhost:5101/";
}
