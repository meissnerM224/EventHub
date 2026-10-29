namespace EventHub.Api.Authentication;

public static class AuthCookie
{
    public const string Name = "eventhub_auth";

    public static CookieOptions Options(DateTimeOffset? expires = null) => new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api",
        Expires = expires,
        IsEssential = true,
    };
}