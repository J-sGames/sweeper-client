using System;

namespace Sweeper.Networking.DTO
{
    [Serializable] public sealed class RegisterRequest { public string loginId; public string password; public string nickname; }
    [Serializable] public sealed class LoginRequest { public string loginId; public string password; }
    [Serializable] public sealed class GoogleLoginRequest { public string idToken; public string nickname; }
    [Serializable] public sealed class RefreshRequest { public string refreshToken; }
    [Serializable] public sealed class LogoutRequest { public string refreshToken; }
    [Serializable] public sealed class AuthTokensResponse { public string accessToken; public string refreshToken; public string accessTokenExpiresAt; public int expiresIn; public UserInfo user; }
    [Serializable] public sealed class UserInfo { public long id; public string nickname; public string email; public string[] authProviders; }
    [Serializable] public sealed class ApiErrorResponse { public string errorCode; public string message; }
    [Serializable] public sealed class ApiEnvelope<T> { public bool success; public T data; public string errorCode; public string message; }
}
