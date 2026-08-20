using System;
using System.Collections;

namespace Sweeper.Networking
{
    public interface IGoogleAuthProvider { IEnumerator SignIn(Action<string, string> completed); }

    public sealed class GoogleAuthProvider : IGoogleAuthProvider
    {
        public IEnumerator SignIn(Action<string, string> completed)
        {
            AuthLog.Warning("Google sign-in requested, but no Google SDK adapter is configured.");
            completed?.Invoke(null, "Google SDK가 설치 및 연결되지 않았습니다.");
            yield break;
        }
    }
}
