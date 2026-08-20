using Sweeper.Networking;
using Sweeper.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sweeper.Editor
{
    public static class MainMenuSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private static readonly Vector2 FieldSize = new(620f, 82f);

        [MenuItem("Sweeper/Rebuild Main Menu Scene")]
        public static void Rebuild()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DestroyAll<GameFlowUI>();
            DestroyAll<AuthUI>();
            DestroyAll<AuthManager>();
            DestroyAll<Canvas>();
            DestroyAll<EventSystem>();

            GameObject serviceRoot = new("Auth Service");
            AuthManager auth = serviceRoot.AddComponent<AuthManager>();
            GameObject uiRoot = new("Authentication UI");
            AuthUI ui = uiRoot.AddComponent<AuthUI>();

            RectTransform canvas = UIObject("Auth Canvas", null);
            Canvas canvasComponent = canvas.gameObject.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = .5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            RectTransform background = UIObject("Background", canvas);
            Stretch(background);
            background.gameObject.AddComponent<Image>().color = new Color(.035f, .055f, .09f, 1f);
            RectTransform safeArea = UIObject("Safe Area", canvas);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();
            Text(safeArea, "Title", "SWEEPER", new Vector2(0, 650), new Vector2(760, 130), 72, FontStyle.Bold);

            RectTransform loginPanel = Panel(safeArea, "Login Panel");
            InputField loginId = Input(loginPanel, "Login ID", "로그인 ID", 230, 30);
            InputField loginPassword = Input(loginPanel, "Password", "비밀번호", 120, 128);
            Button loginButton = Button(loginPanel, "Login Button", "로그인", 0, new Color(.12f, .65f, .7f));
            Button googleButton = Button(loginPanel, "Google Login Button", "Google로 로그인", -110, new Color(.85f, .3f, .25f));
            Button showRegisterButton = Button(loginPanel, "Show Register Button", "회원가입", -220, new Color(.18f, .24f, .34f));

            RectTransform registerPanel = Panel(safeArea, "Register Panel");
            InputField registerId = Input(registerPanel, "Register ID", "로그인 ID (영문/숫자/_ 4~30자)", 330, 30);
            InputField registerPassword = Input(registerPanel, "Register Password", "비밀번호 (10~128자)", 220, 128);
            InputField confirmation = Input(registerPanel, "Password Confirmation", "비밀번호 확인", 110, 128);
            InputField nickname = Input(registerPanel, "Nickname", "닉네임 (2~20자)", 0, 20);
            Button registerButton = Button(registerPanel, "Register Button", "가입하고 로그인", -130, new Color(.12f, .65f, .7f));
            Button showLoginButton = Button(registerPanel, "Back To Login Button", "로그인으로 돌아가기", -240, new Color(.18f, .24f, .34f));

            RectTransform mainPanel = Panel(safeArea, "Authenticated Panel");
            Text welcome = Text(mainPanel, "Welcome Text", "환영합니다.", new Vector2(0, 170), new Vector2(760, 100), 42, FontStyle.Bold);
            Button startButton = Button(mainPanel, "Start Game Button", "게임 시작", 30, new Color(.12f, .65f, .7f));
            Button logoutButton = Button(mainPanel, "Logout Button", "로그아웃", -90, new Color(.18f, .24f, .34f));

            Text status = Text(safeArea, "Status Text", string.Empty, new Vector2(0, -650), new Vector2(820, 150), 28, FontStyle.Normal);
            status.color = new Color(1f, .55f, .55f);

            RectTransform popup = UIObject("Server Response Popup", safeArea);
            Stretch(popup);
            Image popupDim = popup.gameObject.AddComponent<Image>();
            popupDim.color = new Color(0f, 0f, 0f, .72f);
            RectTransform popupDialog = UIObject("Dialog", popup);
            Center(popupDialog, Vector2.zero, new Vector2(820, 520));
            Image dialogImage = popupDialog.gameObject.AddComponent<Image>();
            dialogImage.color = new Color(.07f, .1f, .16f, 1f);
            Text(popupDialog, "Popup Title", "서버 응답", new Vector2(0, 165), new Vector2(700, 80), 38, FontStyle.Bold);
            Text popupMessage = Text(popupDialog, "Response Message", string.Empty, new Vector2(0, 35), new Vector2(690, 190), 29, FontStyle.Normal);
            Button popupCloseButton = Button(popupDialog, "Close Popup Button", "확인", -160, new Color(.12f, .65f, .7f));

            SerializedObject serialized = new(ui);
            Set(serialized, "auth", auth); Set(serialized, "loginPanel", loginPanel.gameObject); Set(serialized, "registerPanel", registerPanel.gameObject); Set(serialized, "mainPanel", mainPanel.gameObject);
            Set(serialized, "loginId", loginId); Set(serialized, "loginPassword", loginPassword); Set(serialized, "registerId", registerId); Set(serialized, "registerPassword", registerPassword); Set(serialized, "registerPasswordConfirmation", confirmation); Set(serialized, "registerNickname", nickname);
            Set(serialized, "statusText", status); Set(serialized, "welcomeText", welcome); Set(serialized, "loginButton", loginButton); Set(serialized, "googleButton", googleButton); Set(serialized, "showRegisterButton", showRegisterButton); Set(serialized, "registerButton", registerButton); Set(serialized, "showLoginButton", showLoginButton); Set(serialized, "startButton", startButton); Set(serialized, "logoutButton", logoutButton);
            Set(serialized, "responsePopup", popup.gameObject); Set(serialized, "responsePopupText", popupMessage); Set(serialized, "responsePopupCloseButton", popupCloseButton);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            registerPanel.gameObject.SetActive(false);
            mainPanel.gameObject.SetActive(false);
            popup.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Authentication UI saved to {ScenePath}");
        }

        private static void DestroyAll<T>() where T : Component { foreach (T item in Object.FindObjectsByType<T>(FindObjectsSortMode.None)) Object.DestroyImmediate(item.gameObject); }
        private static void Set(SerializedObject target, string name, Object value) => target.FindProperty(name).objectReferenceValue = value;
        private static RectTransform Panel(RectTransform parent, string name) { RectTransform rect = UIObject(name, parent); Center(rect, Vector2.zero, new Vector2(900, 1000)); return rect; }

        private static InputField Input(RectTransform parent, string name, string placeholderValue, float y, int limit)
        {
            RectTransform rect = UIObject(name, parent); Center(rect, new Vector2(0, y), FieldSize);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.08f, .12f, .18f, 1f);
            InputField input = rect.gameObject.AddComponent<InputField>();
            Text value = Text(rect, "Text", string.Empty, Vector2.zero, new Vector2(560, 82), 30, FontStyle.Normal, TextAnchor.MiddleLeft);
            Text placeholder = Text(rect, "Placeholder", placeholderValue, Vector2.zero, new Vector2(560, 82), 25, FontStyle.Normal, TextAnchor.MiddleLeft); placeholder.color = new Color(1, 1, 1, .4f);
            input.targetGraphic = image; input.textComponent = value; input.placeholder = placeholder; input.characterLimit = limit;
            return input;
        }

        private static Button Button(RectTransform parent, string name, string label, float y, Color color)
        {
            RectTransform rect = UIObject(name, parent); Center(rect, new Vector2(0, y), FieldSize);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = color;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Text(rect, "Label", label, Vector2.zero, FieldSize, 30, FontStyle.Bold);
            return button;
        }

        private static Text Text(RectTransform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            RectTransform rect = UIObject(name, parent); Center(rect, position, size);
            Text text = rect.gameObject.AddComponent<Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.fontStyle = style; text.alignment = alignment; text.color = Color.white;
            return text;
        }

        private static RectTransform UIObject(string name, RectTransform parent) { GameObject item = new(name, typeof(RectTransform)); RectTransform rect = item.GetComponent<RectTransform>(); if (parent != null) rect.SetParent(parent, false); return rect; }
        private static void Center(RectTransform rect, Vector2 position, Vector2 size) { rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
