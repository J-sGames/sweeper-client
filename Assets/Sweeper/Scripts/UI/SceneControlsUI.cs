using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sweeper.UI
{
    /// <summary>Persistent scene navigation and game settings overlay.</summary>
    public sealed class SceneControlsUI : MonoBehaviour
    {
        private const string VolumePrefsKey = "Sweeper.Settings.MasterVolume";
        private const string MutedPrefsKey = "Sweeper.Settings.Muted";
        private const float DefaultVolume = .8f;

        private Button _mainMenuButton;
        private Button _settingsButton;
        private GameObject _settingsPanel;
        private Slider _volumeSlider;
        private Toggle _muteToggle;
        private Text _volumeValue;
        private float _timeScaleBeforeSettings = 1f;
        private bool _settingsOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCreated()
        {
            if (FindFirstObjectByType<SceneControlsUI>() != null)
                return;

            new GameObject("Scene Controls").AddComponent<SceneControlsUI>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            BuildUI();
            LoadAudioSettings();
            SceneManager.activeSceneChanged += HandleSceneChanged;
            RefreshForScene(SceneManager.GetActiveScene());
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true)
                return;

            if (_settingsOpen)
                CloseSettings();
            else if (SceneManager.GetActiveScene().name == GameFlowUI.PlaySceneName)
                OpenSettings();
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= HandleSceneChanged;
            RestoreTimeScale();
        }

        private void HandleSceneChanged(Scene previous, Scene current)
        {
            CloseSettings();
            RefreshForScene(current);
        }

        private void RefreshForScene(Scene scene)
        {
            _mainMenuButton.gameObject.SetActive(scene.name == GameFlowUI.PlaySceneName);
        }

        private void OpenSettings()
        {
            if (_settingsOpen)
                return;

            _settingsOpen = true;
            _timeScaleBeforeSettings = Time.timeScale;
            if (SceneManager.GetActiveScene().name == GameFlowUI.PlaySceneName)
                Time.timeScale = 0f;
            _settingsPanel.SetActive(true);
            _settingsPanel.transform.SetAsLastSibling();
        }

        private void CloseSettings()
        {
            if (!_settingsOpen)
                return;

            _settingsOpen = false;
            _settingsPanel.SetActive(false);
            RestoreTimeScale();
        }

        private void RestoreTimeScale()
        {
            if (Time.timeScale == 0f)
                Time.timeScale = _timeScaleBeforeSettings;
        }

        private void ReturnToMainMenu()
        {
            CloseSettings();
            SceneManager.LoadScene(GameFlowUI.MainSceneName);
        }

        private void LoadAudioSettings()
        {
            float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefsKey, DefaultVolume));
            bool muted = PlayerPrefs.GetInt(MutedPrefsKey, 0) == 1;
            _volumeSlider.SetValueWithoutNotify(volume);
            _muteToggle.SetIsOnWithoutNotify(muted);
            ApplyAudioSettings(volume, muted, false);
        }

        private void HandleVolumeChanged(float volume)
        {
            ApplyAudioSettings(volume, _muteToggle.isOn, true);
        }

        private void HandleMuteChanged(bool muted)
        {
            ApplyAudioSettings(_volumeSlider.value, muted, true);
        }

        private void ApplyAudioSettings(float volume, bool muted, bool save)
        {
            AudioListener.volume = muted ? 0f : volume;
            _volumeValue.text = $"{Mathf.RoundToInt(volume * 100f)}%";

            if (!save)
                return;

            PlayerPrefs.SetFloat(VolumePrefsKey, volume);
            PlayerPrefs.SetInt(MutedPrefsKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void BuildUI()
        {
            RectTransform canvasRect = CreateRect("Scene Controls Canvas", transform);
            Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            CanvasScaler scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = .5f;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();

            RectTransform safeArea = CreateRect("Safe Area", canvasRect);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            _settingsButton = CreateButton(safeArea, "Settings Button", "설정", new Vector2(-155f, -80f));
            AnchorTopRight(_settingsButton.GetComponent<RectTransform>());
            _settingsButton.onClick.AddListener(OpenSettings);

            _mainMenuButton = CreateButton(safeArea, "Main Menu Button", "메인 메뉴", new Vector2(-155f, -180f));
            AnchorTopRight(_mainMenuButton.GetComponent<RectTransform>());
            _mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            RectTransform overlay = CreateRect("Settings Overlay", canvasRect);
            Stretch(overlay);
            Image dim = overlay.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, .72f);
            _settingsPanel = overlay.gameObject;

            RectTransform dialog = CreateRect("Settings Dialog", overlay);
            Center(dialog, Vector2.zero, new Vector2(820f, 620f));
            Image dialogImage = dialog.gameObject.AddComponent<Image>();
            dialogImage.color = new Color(.07f, .1f, .16f, 1f);

            CreateText(dialog, "Title", "게임 설정", new Vector2(0f, 220f), new Vector2(700f, 90f), 44, FontStyle.Bold);
            CreateText(dialog, "Volume Label", "전체 음량", new Vector2(-240f, 80f), new Vector2(220f, 70f), 30, FontStyle.Bold);

            _volumeSlider = CreateSlider(dialog, new Vector2(40f, 80f));
            _volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
            _volumeValue = CreateText(dialog, "Volume Value", "80%", new Vector2(300f, 80f), new Vector2(130f, 70f), 28, FontStyle.Normal);

            _muteToggle = CreateToggle(dialog, new Vector2(-10f, -45f));
            _muteToggle.onValueChanged.AddListener(HandleMuteChanged);

            Button closeButton = CreateButton(dialog, "Close Button", "닫기", new Vector2(0f, -205f), new Vector2(620f, 82f));
            closeButton.onClick.AddListener(CloseSettings);
            _settingsPanel.SetActive(false);
        }

        private static Button CreateButton(RectTransform parent, string name, string label, Vector2 position, Vector2? size = null)
        {
            RectTransform rect = CreateRect(name, parent);
            Center(rect, position, size ?? new Vector2(260f, 76f));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.12f, .65f, .7f, .96f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(rect, "Label", label, Vector2.zero, rect.sizeDelta, 28, FontStyle.Bold);
            return button;
        }

        private static Slider CreateSlider(RectTransform parent, Vector2 position)
        {
            RectTransform root = CreateRect("Master Volume", parent);
            Center(root, position, new Vector2(390f, 60f));
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            RectTransform background = CreateRect("Background", root);
            Center(background, Vector2.zero, new Vector2(390f, 20f));
            background.gameObject.AddComponent<Image>().color = new Color(.18f, .24f, .34f, 1f);

            RectTransform fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(5f, 20f);
            fillArea.offsetMax = new Vector2(-5f, -20f);
            RectTransform fill = CreateRect("Fill", fillArea);
            Stretch(fill);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(.12f, .65f, .7f, 1f);

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(15f, 0f);
            handleArea.offsetMax = new Vector2(-15f, 0f);
            RectTransform handle = CreateRect("Handle", handleArea);
            Center(handle, Vector2.zero, new Vector2(38f, 38f));
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Toggle CreateToggle(RectTransform parent, Vector2 position)
        {
            RectTransform root = CreateRect("Mute Toggle", parent);
            Center(root, position, new Vector2(440f, 75f));
            Toggle toggle = root.gameObject.AddComponent<Toggle>();

            RectTransform box = CreateRect("Background", root);
            box.anchorMin = box.anchorMax = new Vector2(0f, .5f);
            box.anchoredPosition = new Vector2(30f, 0f);
            box.sizeDelta = new Vector2(54f, 54f);
            Image background = box.gameObject.AddComponent<Image>();
            background.color = new Color(.18f, .24f, .34f, 1f);

            RectTransform checkmark = CreateRect("Checkmark", box);
            Stretch(checkmark);
            checkmark.offsetMin = new Vector2(10f, 10f);
            checkmark.offsetMax = new Vector2(-10f, -10f);
            Image checkmarkImage = checkmark.gameObject.AddComponent<Image>();
            checkmarkImage.color = new Color(.12f, .65f, .7f, 1f);
            toggle.targetGraphic = background;
            toggle.graphic = checkmarkImage;

            Text label = CreateText(root, "Label", "음소거", new Vector2(70f, 0f), new Vector2(290f, 70f), 30, FontStyle.Normal, TextAnchor.MiddleLeft);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0f, .5f);
            return toggle;
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

        private static void AnchorTopRight(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
