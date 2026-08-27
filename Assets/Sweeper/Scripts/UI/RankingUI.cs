using System.Collections;
using Sweeper.Networking;
using SweeperClient.DTOs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sweeper.UI
{
    /// <summary>Persistent ranking button and paged ranking overlay shared by every scene.</summary>
    public sealed class RankingUI : MonoBehaviour
    {
        private const int PageSize = 10;
        private static readonly Color NormalRowColor = new(1f, 1f, 1f, .025f);
        private static readonly Color HighlightRowColor = new(.12f, .65f, .7f, .35f);
        private static bool _gameOverAvailable;

        public static RankingUI Instance { get; private set; }

        private readonly RankingRow[] _rows = new RankingRow[PageSize];
        private GameObject _overlay;
        private Button _openButton;
        private Button _previousButton;
        private Button _nextButton;
        private Text _pageText;
        private Text _statusText;
        private int _currentPage = 1;
        private bool _requestInProgress;
        private bool _isOpen;
        private float _timeScaleBeforeOpen = 1f;
        private int _highlightRank = -1;
        private string _highlightName;
        private int _highlightScore;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCreated()
        {
            if (FindFirstObjectByType<RankingUI>() != null)
                return;

            new GameObject("Ranking UI").AddComponent<RankingUI>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
            SceneManager.activeSceneChanged += HandleSceneChanged;
            RefreshButtonVisibility();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            SceneManager.activeSceneChanged -= HandleSceneChanged;
            RestoreTimeScale();
        }

        private void HandleSceneChanged(Scene previous, Scene current)
        {
            Close();
            _gameOverAvailable = false;
            RefreshButtonVisibility();
        }

        public static void SetGameOverAvailable(bool available)
        {
            _gameOverAvailable = available;
            Instance?.RefreshButtonVisibility();
        }

        private void RefreshButtonVisibility()
        {
            if (_openButton == null)
                return;

            string sceneName = SceneManager.GetActiveScene().name;
            bool isAvailable = sceneName == GameFlowUI.MainSceneName ||
                sceneName == GameFlowUI.PlaySceneName && _gameOverAvailable;
            _openButton.gameObject.SetActive(isAvailable);
            if (!isAvailable)
                Close();
        }

        private void Open()
        {
            _highlightRank = -1;
            _highlightName = null;
            _highlightScore = 0;
            OpenPage(1);
        }

        public void ShowRank(int rank, string playerName, int score)
        {
            if (rank < 1)
                return;

            _highlightRank = rank;
            _highlightName = playerName;
            _highlightScore = score;
            OpenPage((rank - 1) / PageSize + 1);
        }

        private void OpenPage(int page)
        {
            if (_isOpen)
            {
                LoadPage(page);
                return;
            }

            _isOpen = true;
            _timeScaleBeforeOpen = Time.timeScale;
            if (SceneManager.GetActiveScene().name == GameFlowUI.PlaySceneName)
                Time.timeScale = 0f;
            _overlay.SetActive(true);
            _overlay.transform.SetAsLastSibling();
            LoadPage(page);
        }

        private void Close()
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            _overlay.SetActive(false);
            RestoreTimeScale();
        }

        private void RestoreTimeScale()
        {
            if (Time.timeScale == 0f)
                Time.timeScale = _timeScaleBeforeOpen;
        }

        private void LoadPage(int page)
        {
            if (_requestInProgress || page < 1)
                return;

            StartCoroutine(RequestPage(page));
        }

        private IEnumerator RequestPage(int page)
        {
            _requestInProgress = true;
            SetNavigation(false, false);
            SetStatus("랭킹을 불러오는 중입니다.");
            ClearRows();

            while (AuthManager.Instance == null)
                yield return null;

            ApiResult<RankingPageResponse> result = null;
            string path = $"api/result/ranking?page={page}&pageSize={PageSize}";
            yield return AuthManager.Instance.Api.Get<RankingPageResponse>(
                path,
                value => result = value);

            _requestInProgress = false;
            if (!_isOpen)
                yield break;

            if (result == null || !result.IsSuccess || result.Response == null)
            {
                string message = !string.IsNullOrWhiteSpace(result?.Error)
                    ? result.Error
                    : result == null
                        ? "랭킹 요청 결과를 받지 못했습니다."
                        : $"랭킹을 불러오지 못했습니다. ({result.StatusCode})";
                SetStatus(message);
                SetNavigation(page > 1, false);
                yield break;
            }

            RankingPageResponse response = result.Response;
            _currentPage = response.page > 0 ? response.page : page;
            RenderRows(response.items);
            _pageText.text = $"{_currentPage} 페이지";
            bool hasItems = response.items != null && response.items.Length > 0;
            SetStatus(hasItems ? string.Empty : "등록된 랭킹이 없습니다.");
            SetNavigation(_currentPage > 1, response.hasNext);
        }

        private void RenderRows(RankingItemResponse[] items)
        {
            ClearRows();
            if (items == null)
                return;

            int count = Mathf.Min(items.Length, _rows.Length);
            for (int index = 0; index < count; index++)
            {
                RankingItemResponse item = items[index];
                if (item == null)
                    continue;

                string playerName = string.IsNullOrWhiteSpace(item.name)
                    ? "-"
                    : item.name;
                _rows[index].Rank.text = $"{item.rank}위";
                _rows[index].Name.text = playerName;
                _rows[index].Score.text = item.score.ToString("N0");
                bool isHighlighted = item.rank == _highlightRank &&
                    item.score == _highlightScore &&
                    string.Equals(item.name, _highlightName, System.StringComparison.Ordinal);
                _rows[index].Background.color = isHighlighted
                    ? HighlightRowColor
                    : NormalRowColor;
            }
        }

        private void ClearRows()
        {
            for (int index = 0; index < _rows.Length; index++)
            {
                _rows[index].Rank.text = string.Empty;
                _rows[index].Name.text = string.Empty;
                _rows[index].Score.text = string.Empty;
                _rows[index].Background.color = NormalRowColor;
            }
        }

        private void SetNavigation(bool canGoPrevious, bool canGoNext)
        {
            _previousButton.interactable = canGoPrevious;
            _nextButton.interactable = canGoNext;
        }

        private void SetStatus(string message)
        {
            _statusText.text = message ?? string.Empty;
        }

        private void BuildUI()
        {
            RectTransform canvasRect = CreateRect("Ranking Canvas", transform);
            Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1100;
            CanvasScaler scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = .5f;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();

            RectTransform safeArea = CreateRect("Safe Area", canvasRect);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            _openButton = CreateButton(
                safeArea,
                "Open Ranking Button",
                "랭킹",
                new Vector2(155f, -80f),
                new Vector2(260f, 76f));
            RectTransform openRect = _openButton.GetComponent<RectTransform>();
            openRect.anchorMin = openRect.anchorMax = new Vector2(0f, 1f);
            _openButton.onClick.AddListener(Open);

            RectTransform overlay = CreateRect("Ranking Overlay", safeArea);
            Stretch(overlay);
            overlay.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, .78f);
            _overlay = overlay.gameObject;

            RectTransform dialog = CreateRect("Ranking Dialog", overlay);
            dialog.anchorMin = new Vector2(.04f, .04f);
            dialog.anchorMax = new Vector2(.96f, .96f);
            dialog.offsetMin = Vector2.zero;
            dialog.offsetMax = Vector2.zero;
            dialog.gameObject.AddComponent<Image>().color = new Color(.07f, .1f, .16f, 1f);

            VerticalLayoutGroup dialogLayout = dialog.gameObject.AddComponent<VerticalLayoutGroup>();
            dialogLayout.padding = new RectOffset(42, 42, 38, 38);
            dialogLayout.spacing = 10f;
            dialogLayout.childAlignment = TextAnchor.UpperCenter;
            dialogLayout.childControlWidth = true;
            dialogLayout.childControlHeight = true;
            dialogLayout.childForceExpandWidth = true;
            dialogLayout.childForceExpandHeight = false;

            CreateLayoutText(dialog, "Title", "랭킹", 48, FontStyle.Bold, TextAnchor.MiddleCenter, 60f, 82f);
            CreateRankingRow(dialog, "Ranking Header", "순위", "플레이어", "점수", FontStyle.Bold, 26, 42f, 54f);

            RectTransform rowsContainer = CreateRect("Ranking Rows", dialog);
            AddLayoutElement(rowsContainer, 280f, -1f, 1f);
            VerticalLayoutGroup rowsLayout = rowsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 4f;
            rowsLayout.childAlignment = TextAnchor.UpperCenter;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = true;

            for (int index = 0; index < _rows.Length; index++)
            {
                _rows[index] = CreateRankingRow(
                    rowsContainer,
                    $"Ranking Row {index + 1:00}",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    FontStyle.Normal,
                    28,
                    28f,
                    -1f);
            }

            _statusText = CreateLayoutText(dialog, "Status", string.Empty, 25, FontStyle.Normal, TextAnchor.MiddleCenter, 38f, 55f);

            RectTransform navigation = CreateRect("Navigation", dialog);
            AddLayoutElement(navigation, 58f, 70f);
            HorizontalLayoutGroup navigationLayout = navigation.gameObject.AddComponent<HorizontalLayoutGroup>();
            navigationLayout.spacing = 16f;
            navigationLayout.childAlignment = TextAnchor.MiddleCenter;
            navigationLayout.childControlWidth = true;
            navigationLayout.childControlHeight = true;
            navigationLayout.childForceExpandWidth = true;
            navigationLayout.childForceExpandHeight = true;
            _previousButton = CreateLayoutButton(navigation, "Previous Button", "이전", 1f);
            _pageText = CreateLayoutText(navigation, "Page", "1 페이지", 27, FontStyle.Bold, TextAnchor.MiddleCenter, 0f, -1f, 1.2f);
            _nextButton = CreateLayoutButton(navigation, "Next Button", "다음", 1f);

            Button closeButton = CreateLayoutButton(dialog, "Close Button", "닫기", 1f, 58f, 70f);

            _previousButton.onClick.AddListener(() => LoadPage(_currentPage - 1));
            _nextButton.onClick.AddListener(() => LoadPage(_currentPage + 1));
            closeButton.onClick.AddListener(Close);
            _overlay.SetActive(false);
        }

        private static Button CreateButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent);
            Center(rect, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.12f, .65f, .7f, .96f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(rect, "Label", label, Vector2.zero, size, 28, FontStyle.Bold);
            return button;
        }

        private static Button CreateLayoutButton(RectTransform parent, string name, string label, float flexibleWidth, float minHeight = 0f, float preferredHeight = -1f)
        {
            RectTransform rect = CreateRect(name, parent);
            LayoutElement layout = AddLayoutElement(rect, minHeight, preferredHeight);
            layout.flexibleWidth = flexibleWidth;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.12f, .65f, .7f, .96f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(rect, "Label", label, Vector2.zero, Vector2.zero, 28, FontStyle.Bold);
            Stretch(text.rectTransform);
            EnableBestFit(text, 14, 28);
            return button;
        }

        private static Text CreateText(RectTransform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            RectTransform rect = CreateRect(name, parent);
            Center(rect, position, size);
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static Text CreateLayoutText(RectTransform parent, string name, string value, int fontSize, FontStyle style, TextAnchor alignment, float minHeight, float preferredHeight, float flexibleWidth = 0f)
        {
            RectTransform rect = CreateRect(name, parent);
            LayoutElement layout = AddLayoutElement(rect, minHeight, preferredHeight);
            layout.flexibleWidth = flexibleWidth;
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            EnableBestFit(text, 14, fontSize);
            return text;
        }

        private static RankingRow CreateRankingRow(RectTransform parent, string name, string rank, string playerName, string score, FontStyle style, int fontSize, float minHeight, float preferredHeight)
        {
            RectTransform row = CreateRect(name, parent);
            AddLayoutElement(row, minHeight, preferredHeight, preferredHeight < 0f ? 1f : 0f);
            Image background = row.gameObject.AddComponent<Image>();
            background.color = NormalRowColor;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            Text rankText = CreateColumnText(row, "Rank", rank, fontSize, style, TextAnchor.MiddleCenter, 1f);
            Text nameText = CreateColumnText(row, "Player Name", playerName, fontSize, style, TextAnchor.MiddleLeft, 4f);
            Text scoreText = CreateColumnText(row, "Score", score, fontSize, style, TextAnchor.MiddleRight, 2f);
            return new RankingRow(background, rankText, nameText, scoreText);
        }

        private static Text CreateColumnText(RectTransform parent, string name, string value, int fontSize, FontStyle style, TextAnchor alignment, float flexibleWidth)
        {
            Text text = CreateLayoutText(parent, name, value, fontSize, style, alignment, 0f, -1f, flexibleWidth);
            text.rectTransform.GetComponent<LayoutElement>().flexibleHeight = 1f;
            return text;
        }

        private static LayoutElement AddLayoutElement(RectTransform rect, float minHeight, float preferredHeight, float flexibleHeight = 0f)
        {
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = minHeight;
            layout.preferredHeight = preferredHeight;
            layout.flexibleHeight = flexibleHeight;
            return layout;
        }

        private static void EnableBestFit(Text text, int minimumSize, int maximumSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minimumSize;
            text.resizeTextMaxSize = maximumSize;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject item = new(name, typeof(RectTransform));
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private sealed class RankingRow
        {
            public RankingRow(Image background, Text rank, Text name, Text score)
            {
                Background = background;
                Rank = rank;
                Name = name;
                Score = score;
            }

            public Image Background { get; }
            public Text Rank { get; }
            public Text Name { get; }
            public Text Score { get; }
        }
    }
}
