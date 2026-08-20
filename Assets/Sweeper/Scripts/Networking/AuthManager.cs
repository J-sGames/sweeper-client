using System;
using System.Collections;
using Sweeper.Networking.DTO;
using UnityEngine;

namespace Sweeper.Networking
{
    public enum AuthState { Checking, SignedOut, Busy, SignedIn }

    public sealed class AuthManager : MonoBehaviour
    {
        [SerializeField] private string baseUrl = "http://localhost:5065";
        public static AuthManager Instance { get; private set; }
        public AuthState State { get; private set; } = AuthState.Checking;
        public TokenStorage Tokens { get; private set; }
        public ApiClient Api { get; private set; }
        public bool IsSignedIn => State == AuthState.SignedIn;
        public string AccessToken => Tokens?.AccessToken;
        public UserInfo CurrentUser => Tokens?.User;
        public event Action<AuthState, string> StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                AuthLog.Info("Duplicate AuthManager was discarded.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Tokens = new TokenStorage();
            Api = new ApiClient(baseUrl, Tokens);
            AuthLog.Info($"AuthManager initialized (server={baseUrl}).");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        private IEnumerator Start() { yield return AutoLogin(); }

        public IEnumerator AutoLogin()
        {
            SetState(AuthState.Checking);
            if (!Tokens.HasRefreshToken) { SetState(AuthState.SignedOut); yield break; }
            bool success = false;
            yield return Api.RefreshSingleFlight(value => success = value);
            if (!success) { Tokens.Clear(); SetState(AuthState.SignedOut, "로그인 정보가 만료되었습니다. 다시 로그인해 주세요."); yield break; }
            yield return LoadMe();
        }

        public IEnumerator Login(string loginId, string password)
        {
            string validation = AuthValidation.Login(loginId, password);
            if (validation != null) { SetState(AuthState.SignedOut, validation); yield break; }
            yield return Authenticate("api/auth/login", new LoginRequest { loginId = loginId, password = password });
        }

        public IEnumerator Register(string loginId, string password, string confirmation, string nickname)
        {
            string validation = AuthValidation.Register(loginId, password, confirmation, nickname);
            if (validation != null) { SetState(AuthState.SignedOut, validation); yield break; }
            yield return Authenticate("api/auth/register", new RegisterRequest { loginId = loginId, password = password, nickname = nickname.Trim() });
        }

        public IEnumerator GoogleLogin(string idToken, string nickname) => Authenticate("api/auth/google", new GoogleLoginRequest { idToken = idToken, nickname = nickname?.Trim() });

        private IEnumerator Authenticate<T>(string path, T request)
        {
            SetState(AuthState.Busy);
            ApiResult<AuthTokensResponse> result = null;
            yield return Api.Post<T, AuthTokensResponse>(path, request, value => result = value);
            if (result != null && result.IsSuccess)
            {
                if (result.Response == null ||
                    string.IsNullOrWhiteSpace(result.Response.accessToken) ||
                    string.IsNullOrWhiteSpace(result.Response.refreshToken))
                {
                    AuthLog.Error("Authentication response did not contain both accessToken and refreshToken.");
                    SetState(AuthState.SignedOut, "서버 응답에 로그인 토큰이 포함되어 있지 않습니다.");
                    yield break;
                }
                if (Tokens.Replace(result.Response)) { SetState(AuthState.SignedIn); yield break; }
                SetState(AuthState.SignedOut, "로그인 정보를 안전하게 저장하지 못했습니다.");
                yield break;
            }
            SetState(AuthState.SignedOut, AuthErrorMessages.Get(result?.ErrorCode, result?.StatusCode ?? 0, result?.Error));
        }

        private IEnumerator LoadMe()
        {
            ApiResult<UserInfo> result = null;
            yield return Api.Get<UserInfo>("api/auth/me", value => result = value, true);
            if (result != null && result.IsSuccess && result.Response != null &&
                result.Response.id > 0 && !string.IsNullOrWhiteSpace(result.Response.nickname))
            {
                Tokens.SetUser(result.Response);
                SetState(AuthState.SignedIn);
            }
            else
            {
                Tokens.Clear();
                string message = result != null && result.IsSuccess
                    ? "서버에서 올바른 사용자 정보를 받지 못했습니다."
                    : AuthErrorMessages.Get(result?.ErrorCode, result?.StatusCode ?? 0, result?.Error);
                SetState(AuthState.SignedOut, message);
            }
        }

        public IEnumerator Logout()
        {
            SetState(AuthState.Busy);
            if (Tokens.TryGetRefreshToken(out string refreshToken))
                yield return Api.Post<LogoutRequest, ApiErrorResponse>("api/auth/logout", new LogoutRequest { refreshToken = refreshToken }, _ => { });
            Tokens.Clear();
            SetState(AuthState.SignedOut);
        }

        private void SetState(AuthState state, string message = null)
        {
            AuthState previous = State;
            State = state;
            AuthLog.Info($"State {previous} -> {state}" +
                         (string.IsNullOrWhiteSpace(message) ? string.Empty : " (user message available)"));
            StateChanged?.Invoke(state, message);
        }
    }
}
