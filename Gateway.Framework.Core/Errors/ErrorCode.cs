namespace Gateway.Framework.Core.Errors;

public enum ErrorCode
{
    Unknown = 0,
    ValidationFailed = 1000,
    Unauthorized = 2000,
    Forbidden = 2001,
    NotFound = 3000,
    Conflict = 3001,
    RateLimitExceeded = 4000,
    DownstreamUnavailable = 5000,
    DownstreamTimeout = 5001,
    InternalError = 9000
}
