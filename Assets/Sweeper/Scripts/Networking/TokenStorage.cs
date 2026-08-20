using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Sweeper.Networking.DTO;
using UnityEngine;

namespace Sweeper.Networking
{
    public sealed class TokenStorage
    {
        private const string CredentialName = "Sweeper.RefreshToken";
        public string AccessToken { get; private set; }
        public DateTime AccessTokenExpiresAtUtc { get; private set; }
        public UserInfo User { get; private set; }
        public bool HasRefreshToken => TryGetRefreshToken(out _);
        public bool TryGetRefreshToken(out string token) => SecureCredentialStore.TryRead(CredentialName, out token);

        public bool Replace(AuthTokensResponse response)
        {
            Clear();
            if (response == null || string.IsNullOrWhiteSpace(response.accessToken) || string.IsNullOrWhiteSpace(response.refreshToken)) return false;
            if (!SecureCredentialStore.TryWrite(CredentialName, response.refreshToken))
            {
                AuthLog.Error("Refresh token could not be stored in the OS-protected storage.");
                return false;
            }
            if (!SecureCredentialStore.TryRead(CredentialName, out string savedToken) ||
                savedToken != response.refreshToken)
            {
                AuthLog.Error("Refresh token verification failed after secure storage write.");
                SecureCredentialStore.Delete(CredentialName);
                return false;
            }
            AccessToken = response.accessToken;
            User = response.user;
            if (DateTime.TryParse(response.accessTokenExpiresAt, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expiry))
                AccessTokenExpiresAtUtc = expiry.ToUniversalTime();
            else if (response.expiresIn > 0)
                AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(response.expiresIn);
            else
                AccessTokenExpiresAtUtc = DateTime.MinValue;
            return true;
        }

        public void SetUser(UserInfo user) => User = user;
        public void Clear() { AccessToken = null; AccessTokenExpiresAtUtc = default; User = null; SecureCredentialStore.Delete(CredentialName); }
    }

    internal static class SecureCredentialStore
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private const int ProtectUiForbidden = 0x1;
        private const string FileName = "refresh-token.dat";

        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public int Size;
            public IntPtr Data;
        }

        [DllImport("Crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CryptProtectData(
            ref DataBlob input,
            string description,
            IntPtr optionalEntropy,
            IntPtr reserved,
            IntPtr prompt,
            int flags,
            out DataBlob output);

        [DllImport("Crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CryptUnprotectData(
            ref DataBlob input,
            IntPtr description,
            IntPtr optionalEntropy,
            IntPtr reserved,
            IntPtr prompt,
            int flags,
            out DataBlob output);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr memory);

        private static string StoragePath => Path.Combine(
            Application.persistentDataPath,
            "Auth",
            FileName);

        public static bool TryWrite(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            byte[] plainBytes = Encoding.UTF8.GetBytes(value);
            DataBlob input = CreateBlob(plainBytes);
            try
            {
                if (!CryptProtectData(ref input, key, IntPtr.Zero, IntPtr.Zero,
                        IntPtr.Zero, ProtectUiForbidden, out DataBlob output))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                try
                {
                    byte[] encrypted = CopyBlob(output);
                    string directory = Path.GetDirectoryName(StoragePath);
                    Directory.CreateDirectory(directory);
                    File.WriteAllBytes(StoragePath, encrypted);
                    AuthLog.Info("Refresh token saved with Windows DPAPI protection.");
                    return true;
                }
                finally { if (output.Data != IntPtr.Zero) LocalFree(output.Data); }
            }
            catch (Exception exception)
            {
                AuthLog.Error($"DPAPI write failed: {exception.GetType().Name}: {exception.Message}");
                return false;
            }
            finally { FreeBlob(input); Array.Clear(plainBytes, 0, plainBytes.Length); }
        }

        public static bool TryRead(string key, out string value)
        {
            value = null;
            if (!File.Exists(StoragePath)) return false;
            byte[] encrypted = null;
            DataBlob input = default;
            try
            {
                encrypted = File.ReadAllBytes(StoragePath);
                input = CreateBlob(encrypted);
                if (!CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero,
                        IntPtr.Zero, IntPtr.Zero, ProtectUiForbidden, out DataBlob output))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                try
                {
                    byte[] plainBytes = CopyBlob(output);
                    try { value = Encoding.UTF8.GetString(plainBytes); }
                    finally { Array.Clear(plainBytes, 0, plainBytes.Length); }
                }
                finally { if (output.Data != IntPtr.Zero) LocalFree(output.Data); }
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception exception)
            {
                AuthLog.Error($"DPAPI read failed: {exception.GetType().Name}: {exception.Message}");
                return false;
            }
            finally
            {
                FreeBlob(input);
                if (encrypted != null) Array.Clear(encrypted, 0, encrypted.Length);
            }
        }

        public static void Delete(string key)
        {
            try { if (File.Exists(StoragePath)) File.Delete(StoragePath); }
            catch (Exception exception) { AuthLog.Warning($"Secure token deletion failed: {exception.Message}"); }
        }

        private static DataBlob CreateBlob(byte[] bytes)
        {
            DataBlob blob = new() { Size = bytes.Length, Data = Marshal.AllocHGlobal(bytes.Length) };
            Marshal.Copy(bytes, 0, blob.Data, bytes.Length);
            return blob;
        }

        private static byte[] CopyBlob(DataBlob blob)
        {
            byte[] bytes = new byte[blob.Size];
            Marshal.Copy(blob.Data, bytes, 0, blob.Size);
            return bytes;
        }

        private static void FreeBlob(DataBlob blob)
        {
            if (blob.Data != IntPtr.Zero) Marshal.FreeHGlobal(blob.Data);
        }
#else
        public static bool TryWrite(string key, string value) { Debug.LogError("Native secure token storage is required for this platform."); return false; }
        public static bool TryRead(string key, out string value) { value = null; return false; }
        public static void Delete(string key) { }
#endif
    }
}
