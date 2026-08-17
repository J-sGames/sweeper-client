using System;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Sweeper.EditorTools
{
    public sealed class HttpCheckWindow : EditorWindow
    {
        private const string DefaultUrl = "http://localhost:8080/health";
        private const int DefaultTimeoutSeconds = 10;

        private string _url = DefaultUrl;
        private string _method = UnityWebRequest.kHttpVerbGET;
        private string _requestBody = string.Empty;
        private int _timeoutSeconds = DefaultTimeoutSeconds;
        private string _result = "Enter an HTTP or HTTPS URL, then select Send Request.";
        private Vector2 _scrollPosition;
        private UnityWebRequest _request;
        private UnityWebRequestAsyncOperation _operation;
        private Stopwatch _stopwatch;

        [MenuItem("Tools/Sweeper/HTTP Checker")]
        public static void Open()
        {
            HttpCheckWindow window = GetWindow<HttpCheckWindow>("HTTP Checker");
            window.minSize = new Vector2(520f, 420f);
        }

        private bool IsRunning => _operation != null && !_operation.isDone;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("HTTP Request", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(IsRunning))
            {
                _url = EditorGUILayout.TextField("URL", _url);
                _method = EditorGUILayout.Popup("Method", _method == UnityWebRequest.kHttpVerbGET ? 0 : 1,
                    new[] { UnityWebRequest.kHttpVerbGET, UnityWebRequest.kHttpVerbPOST }) == 0
                    ? UnityWebRequest.kHttpVerbGET
                    : UnityWebRequest.kHttpVerbPOST;
                _timeoutSeconds = EditorGUILayout.IntSlider("Timeout (seconds)", _timeoutSeconds, 1, 60);

                if (_method == UnityWebRequest.kHttpVerbPOST)
                {
                    EditorGUILayout.LabelField("JSON Body");
                    _requestBody = EditorGUILayout.TextArea(_requestBody, GUILayout.MinHeight(90f));
                }
            }

            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (IsRunning)
                {
                    if (GUILayout.Button("Cancel", GUILayout.Height(28f)))
                    {
                        CancelRequest();
                    }
                }
                else if (GUILayout.Button("Send Request", GUILayout.Height(28f)))
                {
                    SendRequest();
                }

                if (GUILayout.Button("Clear", GUILayout.Width(80f), GUILayout.Height(28f)))
                {
                    _result = string.Empty;
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Response", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.TextArea(_result, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void Update()
        {
            if (_operation == null || !_operation.isDone)
            {
                return;
            }

            CompleteRequest();
            Repaint();
        }

        private void OnDisable()
        {
            DisposeRequest();
        }

        private void SendRequest()
        {
            if (!Uri.TryCreate(_url.Trim(), UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                _result = "Invalid URL. Enter a full HTTP or HTTPS URL.";
                return;
            }

            DisposeRequest();

            if (_method == UnityWebRequest.kHttpVerbPOST)
            {
                byte[] body = Encoding.UTF8.GetBytes(_requestBody ?? string.Empty);
                _request = new UnityWebRequest(uri.AbsoluteUri, UnityWebRequest.kHttpVerbPOST)
                {
                    uploadHandler = new UploadHandlerRaw(body),
                    downloadHandler = new DownloadHandlerBuffer()
                };
                _request.SetRequestHeader("Content-Type", "application/json");
            }
            else
            {
                _request = UnityWebRequest.Get(uri.AbsoluteUri);
            }

            _request.timeout = _timeoutSeconds;
            _result = $"Sending {_method} {uri.AbsoluteUri} ...";
            _stopwatch = Stopwatch.StartNew();
            _operation = _request.SendWebRequest();
        }

        private void CompleteRequest()
        {
            _stopwatch?.Stop();

            StringBuilder response = new StringBuilder();
            response.AppendLine($"{_method} {_request.url}");
            response.AppendLine($"Status: {_request.responseCode}");
            response.AppendLine($"Result: {_request.result}");
            response.AppendLine($"Elapsed: {_stopwatch?.ElapsedMilliseconds ?? 0} ms");

            if (!string.IsNullOrWhiteSpace(_request.error))
            {
                response.AppendLine($"Error: {_request.error}");
            }

            response.AppendLine();
            response.AppendLine("Response Headers");

            var headers = _request.GetResponseHeaders();
            if (headers == null || headers.Count == 0)
            {
                response.AppendLine("(none)");
            }
            else
            {
                foreach (var header in headers)
                {
                    response.AppendLine($"{header.Key}: {header.Value}");
                }
            }

            response.AppendLine();
            response.AppendLine("Response Body");
            response.AppendLine(string.IsNullOrEmpty(_request.downloadHandler?.text)
                ? "(empty)"
                : _request.downloadHandler.text);

            _result = response.ToString();
            DisposeRequest();
        }

        private void CancelRequest()
        {
            _request?.Abort();
            _stopwatch?.Stop();
            _result = $"Request cancelled after {_stopwatch?.ElapsedMilliseconds ?? 0} ms.";
            DisposeRequest();
            Repaint();
        }

        private void DisposeRequest()
        {
            _request?.Dispose();
            _request = null;
            _operation = null;
            _stopwatch = null;
        }
    }
}
