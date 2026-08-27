using System;
using UnityEngine;
using UnityEngine.UI;
using SweeperClient.DTOs;
using Sweeper.Networking;

namespace Sweeper.UI
{
    public sealed class GameOverView : MonoBehaviour, IResultView<ScoreResponse>
    {
        public static bool IsSubmitting { get; private set; }

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
        private GameObject _inputBlocker;
        private bool _submissionInProgress;

        private void Awake()
        {
            if (!string.IsNullOrWhiteSpace(baseUrl))
                _apiClient = new ApiClient(baseUrl.TrimEnd('/'));
            _inputBlocker = CreateInputBlocker();
            SetSubmissionLocked(false);
        }

        private void OnDestroy()
        {
            IsSubmitting = false;
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
            if (_submissionInProgress)
                return;

            if (_apiClient == null)
            {
                _lastError = "SCORE API IS NOT CONFIGURED";
                ((IResultView<ScoreResponse>)this).OnFailed();
                return;
            }

            ShowSubmitting();
            _submissionInProgress = true;
            SetSubmissionLocked(true);
            string playerName = GetSubmissionPlayerName();
            ScoreRequest scoreRequest = new()
            {
                name = playerName,
                score = _finalScore,
                startedTime = _startedTime.ToString("O"),
                endedTime = DateTime.UtcNow.ToString("O")
            };

            StartCoroutine(_apiClient.Post<ScoreRequest, ScoreResponse>(
                "api/result/achieve",
                scoreRequest,
                result =>
                {
                    if (result.IsSuccess)
                    {
                        _submissionInProgress = false;
                        SetSubmissionLocked(false);
                        ((IResultView<ScoreResponse>)this).OnSuccess(result);
                        return;
                    }

                    _submissionInProgress = false;
                    SetSubmissionLocked(false);
                    _lastError = string.IsNullOrWhiteSpace(result.Error)
                        ? $"FAILED TO SEND SCORE ({result.StatusCode})"
                        : result.Error;
                    ((IResultView<ScoreResponse>)this).OnFailed();
                }));
        }

        void IResultView<ScoreResponse>.OnSuccess(ApiResult<ScoreResponse> result)
        {
            ShowSubmissionSucceeded(result.Response?.rank ?? 0);
            if (result.Response != null && result.Response.rank > 0)
            {
                RankingUI ranking = RankingUI.Instance != null
                    ? RankingUI.Instance
                    : FindFirstObjectByType<RankingUI>();
                ranking?.ShowRank(
                    result.Response.rank,
                    string.IsNullOrWhiteSpace(result.Response.name)
                        ? GetSubmissionPlayerName()
                        : result.Response.name,
                    result.Response.score);
            }
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
            RankingUI.SetGameOverAvailable(true);
            ((IResultView<ScoreResponse>)this).OnAwake();
        }

        public void Hide()
        {
            RankingUI.SetGameOverAvailable(false);
            gameObject.SetActive(false);
        }

        public void ShowSubmitting()
        {
            SetSubmissionStatus("SENDING SCORE...", Color.white);
        }

        public void ShowSubmissionSucceeded(int rank = 0)
        {
            string message = rank > 0
                ? $"SCORE SENT  ·  내 순위 {rank:N0}위"
                : "SCORE SENT";
            SetSubmissionStatus(message, new Color(.4f, 1f, .65f));
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

        private static string GetSubmissionPlayerName()
        {
            string authenticatedName = AuthManager.Instance?.CurrentUser?.nickname;
            return string.IsNullOrWhiteSpace(authenticatedName)
                ? GameFlowUI.GetPlayerName()
                : authenticatedName.Trim();
        }

        private void SetSubmissionLocked(bool locked)
        {
            IsSubmitting = locked;
            if (restartButton != null)
                restartButton.interactable = !locked;
            if (mainMenuButton != null)
                mainMenuButton.interactable = !locked;
            if (_inputBlocker != null)
            {
                _inputBlocker.SetActive(locked);
                if (locked)
                    _inputBlocker.transform.SetAsLastSibling();
            }
        }

        private GameObject CreateInputBlocker()
        {
            GameObject blocker = new("Score Submission Input Blocker", typeof(RectTransform));
            RectTransform rect = blocker.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Canvas canvas = blocker.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000;
            blocker.AddComponent<GraphicRaycaster>();
            Image image = blocker.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, .01f);
            image.raycastTarget = true;
            return blocker;
        }
    }
}
