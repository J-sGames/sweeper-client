using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Sweeper.Networking.DTO;
using UnityEngine;
using UnityEngine.Networking;

namespace Sweeper.Networking
{
    public sealed class ApiClient
    {
        private readonly string _baseUrl;
        private readonly TokenStorage _tokens;
        private bool _refreshing;
        private bool _lastRefreshSucceeded;

        public ApiClient(string baseUrl, TokenStorage tokens = null) { _baseUrl = baseUrl.TrimEnd('/'); _tokens = tokens; }
        public IEnumerator Get<T>(string path, Action<ApiResult<T>> done, bool authenticated = false) => Send(path, UnityWebRequest.kHttpVerbGET, null, done, authenticated, true);
        public IEnumerator Post<TRequest, TResponse>(string path, TRequest body, Action<ApiResult<TResponse>> done, bool authenticated = false) => Send(path, UnityWebRequest.kHttpVerbPOST, JsonUtility.ToJson(body), done, authenticated, true);

        private IEnumerator Send<T>(string path, string method, string json, Action<ApiResult<T>> done, bool authenticated, bool retry)
        {
            int requestId = UnityEngine.Random.Range(100000, 999999);
            float startedAt = Time.realtimeSinceStartup;
            AuthLog.Info($"HTTP #{requestId} -> {method} /{path.TrimStart('/')} (auth={authenticated}, retry={!retry})");
            if (!string.IsNullOrWhiteSpace(json))
                AuthLog.Info($"HTTP #{requestId} request: {SanitizeJson(json)}");
            using UnityWebRequest request = CreateRequest(path, method, json, authenticated);
            yield return request.SendWebRequest();
            if (request.responseCode == 401 && authenticated && retry && _tokens != null)
            {
                AuthLog.Warning($"HTTP #{requestId} received 401. Waiting for token refresh.");
                yield return RefreshSingleFlight();
                if (_lastRefreshSucceeded)
                {
                    AuthLog.Info($"HTTP #{requestId} token refresh succeeded. Retrying once.");
                    yield return Send(path, method, json, done, true, false);
                    yield break;
                }
                AuthLog.Warning($"HTTP #{requestId} token refresh failed.");
            }
            ApiResult<T> result = CreateResult<T>(request);
            float elapsedMs = (Time.realtimeSinceStartup - startedAt) * 1000f;
            string summary = $"HTTP #{requestId} <- {request.responseCode} in {elapsedMs:F0}ms";
            if (result.IsSuccess) AuthLog.Info(summary);
            else AuthLog.Warning($"{summary} (errorCode={result.ErrorCode ?? "none"}, transport={request.result})");
            string responseBody = request.downloadHandler?.text;
            if (!string.IsNullOrWhiteSpace(responseBody))
                AuthLog.Info($"HTTP #{requestId} response: {SanitizeJson(responseBody)}");
            done?.Invoke(result);
        }

        public IEnumerator RefreshSingleFlight(Action<bool> done = null)
        {
            if (_refreshing)
            {
                AuthLog.Info("Refresh already in progress; joining the existing request.");
                while (_refreshing) yield return null;
                done?.Invoke(_lastRefreshSucceeded);
                yield break;
            }
            _refreshing = true; _lastRefreshSucceeded = false;
            if (_tokens == null || !_tokens.TryGetRefreshToken(out string refreshToken))
            {
                AuthLog.Warning("Refresh skipped because no refresh token is available.");
                _refreshing = false; done?.Invoke(false); yield break;
            }
            AuthLog.Info("Refreshing access token.");
            using UnityWebRequest request = CreateRequest("api/auth/refresh", UnityWebRequest.kHttpVerbPOST, JsonUtility.ToJson(new RefreshRequest { refreshToken = refreshToken }), false);
            yield return request.SendWebRequest();
            ApiResult<AuthTokensResponse> result = CreateResult<AuthTokensResponse>(request);
            _lastRefreshSucceeded = result.IsSuccess && _tokens.Replace(result.Response);
            if (!_lastRefreshSucceeded) _tokens.Clear();
            AuthLog.Info(_lastRefreshSucceeded
                ? "Token refresh and rotation completed."
                : $"Token refresh failed (status={result.StatusCode}, errorCode={result.ErrorCode ?? "none"}). Local tokens were cleared.");
            _refreshing = false; done?.Invoke(_lastRefreshSucceeded);
        }

        private UnityWebRequest CreateRequest(string path, string method, string json, bool authenticated)
        {
            UnityWebRequest request = new(_baseUrl + "/" + path.TrimStart('/'), method) { downloadHandler = new DownloadHandlerBuffer() };
            if (json != null) { request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)); request.SetRequestHeader("Content-Type", "application/json"); }
            if (authenticated && !string.IsNullOrWhiteSpace(_tokens?.AccessToken)) request.SetRequestHeader("Authorization", "Bearer " + _tokens.AccessToken);
            return request;
        }

        private static ApiResult<T> CreateResult<T>(UnityWebRequest request)
        {
            ApiResult<T> result = new() { StatusCode = request.responseCode, IsSuccess = request.responseCode >= 200 && request.responseCode < 300 };
            string json = request.downloadHandler?.text;
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                result.IsSuccess = false;
                result.Error = request.error;
                return result;
            }
            try
            {
                if (result.IsSuccess && !string.IsNullOrWhiteSpace(json))
                {
                    if (HasJsonProperty(json, "success"))
                    {
                        ApiEnvelope<T> envelope = JsonUtility.FromJson<ApiEnvelope<T>>(json);
                        result.IsSuccess = envelope != null && envelope.success;
                        result.Response = envelope == null ? default : envelope.data;
                        result.ErrorCode = envelope?.errorCode;
                        result.Error = envelope?.message;
                    }
                    else
                    {
                        result.Response = JsonUtility.FromJson<T>(json);
                    }
                }
                else if (!result.IsSuccess && !string.IsNullOrWhiteSpace(json))
                {
                    ApiEnvelope<T> envelope = HasJsonProperty(json, "success")
                        ? JsonUtility.FromJson<ApiEnvelope<T>>(json)
                        : null;
                    ApiErrorResponse error = JsonUtility.FromJson<ApiErrorResponse>(json);
                    result.ErrorCode = !string.IsNullOrWhiteSpace(envelope?.errorCode)
                        ? envelope.errorCode
                        : string.IsNullOrWhiteSpace(error?.errorCode)
                            ? ExtractJsonString(json, "errorCode")
                            : error.errorCode;
                    result.Error = !string.IsNullOrWhiteSpace(envelope?.message)
                        ? envelope.message
                        : string.IsNullOrWhiteSpace(error?.message)
                            ? ExtractValidationMessage(json)
                            : error.message;
                }
            }
            catch (Exception exception) { result.IsSuccess = false; result.Error = exception.Message; }
            if (!result.IsSuccess && string.IsNullOrWhiteSpace(result.Error)) result.Error = request.error;
            return result;
        }

        private static string ExtractValidationMessage(string json)
        {
            string detail = ExtractJsonString(json, "detail");
            if (!string.IsNullOrWhiteSpace(detail)) return detail;

            Match errors = Regex.Match(
                json,
                "\\\"errors\\\"\\s*:\\s*\\{[\\s\\S]*?\\[\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"",
                RegexOptions.CultureInvariant);
            if (errors.Success) return Regex.Unescape(errors.Groups["value"].Value);

            return ExtractJsonString(json, "title");
        }

        private static bool HasJsonProperty(string json, string property) =>
            Regex.IsMatch(
                json,
                $"\\\"{Regex.Escape(property)}\\\"\\s*:",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static string ExtractJsonString(string json, string property)
        {
            Match match = Regex.Match(
                json,
                $"\\\"{Regex.Escape(property)}\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            return match.Success ? Regex.Unescape(match.Groups["value"].Value) : null;
        }

        private static string SanitizeJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;

            string sanitized = json;
            string[] sensitiveProperties =
            {
                "password",
                "passwordConfirmation",
                "accessToken",
                "access_token",
                "refreshToken",
                "refresh_token",
                "idToken",
                "id_token"
            };

            foreach (string property in sensitiveProperties)
            {
                sanitized = Regex.Replace(
                    sanitized,
                    $"(\\\"{Regex.Escape(property)}\\\"\\s*:\\s*\\\")(?:\\\\.|[^\\\"])*(\\\")",
                    "$1[REDACTED]$2",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            const int maxLogLength = 4000;
            return sanitized.Length <= maxLogLength
                ? sanitized
                : sanitized.Substring(0, maxLogLength) + "… [TRUNCATED]";
        }
    }
}
