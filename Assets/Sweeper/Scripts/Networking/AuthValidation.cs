using System.Text.RegularExpressions;
using UnityEngine;

namespace Sweeper.Networking
{
    public static class AuthLog
    {
        private const string Prefix = "[Sweeper.Auth]";
        public static void Info(string message) => Debug.Log($"{Prefix} {message}");
        public static void Warning(string message) => Debug.LogWarning($"{Prefix} {message}");
        public static void Error(string message) => Debug.LogError($"{Prefix} {message}");
    }

    public static class AuthValidation
    {
        private static readonly Regex LoginIdPattern = new("^[A-Za-z0-9_]{4,30}$");
        public static string Register(string loginId, string password, string confirmation, string nickname)
        {
            if (!LoginIdPattern.IsMatch(loginId ?? string.Empty)) return "ID는 영문, 숫자, _만 사용하여 4~30자로 입력해 주세요.";
            if (password == null || password.Length < 10 || password.Length > 128) return "비밀번호는 10~128자로 입력해 주세요.";
            if (password != confirmation) return "비밀번호가 일치하지 않습니다.";
            nickname = nickname?.Trim();
            if (nickname == null || nickname.Length < 2 || nickname.Length > 20) return "닉네임은 2~20자로 입력해 주세요.";
            return null;
        }
        public static string Login(string loginId, string password)
        {
            if (!LoginIdPattern.IsMatch(loginId ?? string.Empty)) return "ID 형식을 확인해 주세요.";
            if (string.IsNullOrEmpty(password)) return "비밀번호를 입력해 주세요.";
            return null;
        }
    }

    public static class AuthErrorMessages
    {
        public static string Get(string code, long statusCode, string fallback = null) => code switch
        {
            "LOGIN_ID_ALREADY_EXISTS" => "이미 사용 중인 ID입니다.",
            "NICKNAME_ALREADY_EXISTS" => "이미 사용 중인 닉네임입니다.",
            "INVALID_CREDENTIALS" => "ID 또는 비밀번호가 올바르지 않습니다.",
            "INVALID_GOOGLE_TOKEN" => "Google 로그인에 실패했습니다. 다시 시도해 주세요.",
            "GOOGLE_NOT_CONFIGURED" => "현재 Google 로그인을 사용할 수 없습니다.",
            "INVALID_REFRESH_TOKEN" => "로그인 정보가 만료되었습니다. 다시 로그인해 주세요.",
            "INVALID_TOKEN" => "인증 정보가 올바르지 않습니다.",
            "USER_NOT_FOUND" => "사용자 정보를 찾을 수 없습니다.",
            _ when statusCode == 0 => "서버에 연결할 수 없습니다. 네트워크 상태를 확인해 주세요.",
            _ when statusCode == 400 => string.IsNullOrWhiteSpace(fallback) ? "입력 내용을 다시 확인해 주세요." : fallback,
            _ when statusCode >= 500 => "서버에 문제가 발생했습니다. 잠시 후 다시 시도해 주세요.",
            _ => string.IsNullOrWhiteSpace(fallback) ? "요청을 처리하지 못했습니다." : fallback
        };
    }
}
