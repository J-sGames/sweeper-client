using System;
using UnityEngine;
using UnityEngine.UI;
using SweeperClient.DTOs;
using Sweeper.Networking;

namespace Sweeper.UI
{
    public sealed class GameOverView : MonoBehaviour, IResultView<ScoreResponse>
    {
        [Header("UI")]
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text submissionStatusText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Score API")]
        [SerializeField] private string baseUrl;

        private ApiClient _apiClient;
        private DateTime _startedTime;
        private int _finalScore;
        private string _lastError;

        private void Awake()
        {
            if (!string.IsNullOrWhiteSpace(baseUrl))
                _apiClient = new ApiClient(baseUrl.TrimEnd('/'));
        }

        public void Configure(
            Action restartRequested,
            Action mainMenuRequested)
        {
            restartButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => restartRequested?.Invoke());
            mainMenuButton.onClick.AddListener(() => mainMenuRequested?.Invoke());
        }

        void IResultView<ScoreResponse>.OnAwake()
        {
            if (_apiClient == null)
            {
                _lastError = "SCORE API IS NOT CONFIGURED";
                ((IResultView<ScoreResponse>)this).OnFailed();
                return;
            }

            ShowSubmitting();
            ScoreRequest scoreRequest = new()
            {
                Name = GameFlowUI.GetPlayerName(),
                Score = _finalScore,
                StartedTime = _startedTime.ToString("O"),
                EndedTime = DateTime.UtcNow.ToString("O")
            };

            StartCoroutine(_apiClient.Post<ScoreRequest, ScoreResponse>(
                "api/result/achieve",
                scoreRequest,
                result =>
                {
                    if (result.IsSuccess)
                    {
                        ((IResultView<ScoreResponse>)this).OnSuccess(result);
                        return;
                    }

                    _lastError = string.IsNullOrWhiteSpace(result.Error)
                        ? $"FAILED TO SEND SCORE ({result.StatusCode})"
                        : result.Error;
                    ((IResultView<ScoreResponse>)this).OnFailed();
                }));
        }

        void IResultView<ScoreResponse>.OnSuccess(ApiResult<ScoreResponse> result)
        {
            ShowSubmissionSucceeded();
        }

        void IResultView<ScoreResponse>.OnFailed()
        {
            ShowSubmissionFailed(_lastError);
        }

        public void Show(int score, DateTime startedTime)
        {
            _finalScore = score;
            _startedTime = startedTime;
            finalScoreText.text = $"SCORE  {score:N0}";
            submissionStatusText.text = string.Empty;
            gameObject.SetActive(true);
            ((IResultView<ScoreResponse>)this).OnAwake();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ShowSubmitting()
        {
            SetSubmissionStatus("SENDING SCORE...", Color.white);
        }

        public void ShowSubmissionSucceeded()
        {
            SetSubmissionStatus("SCORE SENT", new Color(.4f, 1f, .65f));
        }

        public void ShowSubmissionFailed(string message)
        {
            SetSubmissionStatus(
                string.IsNullOrWhiteSpace(message) ? "FAILED TO SEND SCORE" : message,
                new Color(1f, .45f, .45f));
        }

        private void SetSubmissionStatus(string message, Color color)
        {
            if (submissionStatusText == null)
                return;

            submissionStatusText.text = message;
            submissionStatusText.color = color;
        }
    }
}
