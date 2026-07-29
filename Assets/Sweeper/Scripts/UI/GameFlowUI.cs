using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sweeper.UI
{
    public sealed class GameFlowUI : MonoBehaviour
    {
        public const string MainSceneName = "Main";
        public const string PlaySceneName = "Play";

        private static Font _font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != MainSceneName ||
                FindFirstObjectByType<GameFlowUI>() != null)
                return;

            new GameObject("Main Menu UI").AddComponent<GameFlowUI>().BuildMainMenu();
        }

        public static void ShowGameOver()
        {
            if (GameObject.Find("Game Over UI") != null)
                return;

            GameFlowUI view =
                new GameObject("Game Over UI").AddComponent<GameFlowUI>();
            view.BuildGameOverMenu();
        }

        private void BuildMainMenu()
        {
            EnsureEventSystem();
            RectTransform canvas = CreateCanvas("Main Menu Canvas");
            CreateFullScreenImage(
                canvas,
                new Color(.035f, .055f, .09f, 1f));
            RectTransform safeArea = CreateSafeArea(canvas);
            CreateText(
                safeArea,
                "SWEEPER",
                new Vector2(0f, 120f),
                new Vector2(650f, 140f),
                72,
                FontStyle.Bold);
            CreateButton(
                safeArea,
                "START GAME",
                new Vector2(0f, -70f),
                () => SceneManager.LoadScene(PlaySceneName));
        }

        private void BuildGameOverMenu()
        {
            EnsureEventSystem();
            RectTransform canvas = CreateCanvas("Game Over Canvas");
            Canvas canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.sortingOrder = 100;

            CreateFullScreenImage(canvas, new Color(0f, 0f, 0f, .72f));
            RectTransform safeArea = CreateSafeArea(canvas);
            CreateText(
                safeArea,
                "GAME OVER",
                new Vector2(0f, 150f),
                new Vector2(650f, 120f),
                64,
                FontStyle.Bold);
            CreateButton(
                safeArea,
                "RESTART",
                new Vector2(0f, 0f),
                () => SceneManager.LoadScene(PlaySceneName));
            CreateButton(
                safeArea,
                "MAIN MENU",
                new Vector2(0f, -115f),
                () => SceneManager.LoadScene(MainSceneName));
        }

        private static RectTransform CreateCanvas(string objectName)
        {
            GameObject canvasObject = new(
                objectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = .5f;
            return canvasObject.GetComponent<RectTransform>();
        }

        private static void CreateFullScreenImage(
            RectTransform parent,
            Color color)
        {
            GameObject imageObject = new(
                "Background",
                typeof(RectTransform),
                typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            imageObject.GetComponent<Image>().color = color;
        }

        private static RectTransform CreateSafeArea(RectTransform parent)
        {
            GameObject safeAreaObject = new(
                "Safe Area",
                typeof(RectTransform),
                typeof(SafeAreaFitter));
            RectTransform rect = safeAreaObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void CreateText(
            RectTransform parent,
            string value,
            Vector2 position,
            Vector2 size,
            int fontSize,
            FontStyle style)
        {
            GameObject textObject = new(
                value,
                typeof(RectTransform),
                typeof(Text));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = RuntimeFont;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        private static void CreateButton(
            RectTransform parent,
            string label,
            Vector2 position,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new(
                $"{label} Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(520f, 90f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(.12f, .65f, .7f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            CreateText(
                rect,
                label,
                Vector2.zero,
                rect.sizeDelta,
                38,
                FontStyle.Bold);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule =
                eventSystem.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static Font RuntimeFont
        {
            get
            {
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }
    }

    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            Vector2Int screenSize = new(Screen.width, Screen.height);
            if (_lastSafeArea != Screen.safeArea ||
                _lastScreenSize != screenSize)
                ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (_rectTransform == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _rectTransform.anchorMin = new Vector2(
                safeArea.xMin / Screen.width,
                safeArea.yMin / Screen.height);
            _rectTransform.anchorMax = new Vector2(
                safeArea.xMax / Screen.width,
                safeArea.yMax / Screen.height);
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
