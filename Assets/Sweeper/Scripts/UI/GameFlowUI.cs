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
        public const string DefaultPlayerName = "Player";
        public const string PlayerNamePrefsKey = "Sweeper.PlayerName";

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
                new Vector2(0f, 170f),
                new Vector2(650f, 140f),
                72,
                FontStyle.Bold);

            InputField playerNameInput = CreateInputField(
                safeArea,
                "PLAYER NAME",
                new Vector2(0f, 10f));
            playerNameInput.text = PlayerPrefs.GetString(
                PlayerNamePrefsKey,
                DefaultPlayerName);

            CreateButton(
                safeArea,
                "START GAME",
                new Vector2(0f, -120f),
                () =>
                {
                    SavePlayerName(playerNameInput.text);
                    SceneManager.LoadScene(PlaySceneName);
                });
        }

        public static string GetPlayerName()
        {
            string playerName = PlayerPrefs.GetString(
                PlayerNamePrefsKey,
                DefaultPlayerName);
            return string.IsNullOrWhiteSpace(playerName)
                ? DefaultPlayerName
                : playerName.Trim();
        }

        private static void SavePlayerName(string playerName)
        {
            string normalizedName = string.IsNullOrWhiteSpace(playerName)
                ? DefaultPlayerName
                : playerName.Trim();
            PlayerPrefs.SetString(PlayerNamePrefsKey, normalizedName);
            PlayerPrefs.Save();
        }

        internal static RectTransform CreateCanvas(string objectName)
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

        internal static void CreateFullScreenImage(
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

        internal static RectTransform CreateSafeArea(RectTransform parent)
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

        internal static Text CreateText(
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
            return text;
        }

        internal static void CreateButton(
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

        private static InputField CreateInputField(
            RectTransform parent,
            string placeholderValue,
            Vector2 position)
        {
            GameObject inputObject = new(
                "Player Name Input",
                typeof(RectTransform),
                typeof(Image),
                typeof(InputField));
            RectTransform rect = inputObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(520f, 90f);

            Image background = inputObject.GetComponent<Image>();
            background.color = new Color(.08f, .12f, .18f, 1f);

            Text inputText = CreateText(
                rect,
                string.Empty,
                Vector2.zero,
                new Vector2(460f, 90f),
                34,
                FontStyle.Normal);
            inputText.alignment = TextAnchor.MiddleLeft;

            Text placeholder = CreateText(
                rect,
                placeholderValue,
                Vector2.zero,
                new Vector2(460f, 90f),
                30,
                FontStyle.Normal);
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.color = new Color(1f, 1f, 1f, .45f);

            InputField inputField = inputObject.GetComponent<InputField>();
            inputField.targetGraphic = background;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholder;
            inputField.characterLimit = 24;
            inputField.lineType = InputField.LineType.SingleLine;
            return inputField;
        }

        internal static void EnsureEventSystem()
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

}
