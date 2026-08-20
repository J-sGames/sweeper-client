using System.Collections;
using Sweeper.Networking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sweeper.UI
{
    public sealed class AuthUI : MonoBehaviour
    {
        [SerializeField] private AuthManager auth;
        [SerializeField] private GameObject loginPanel, registerPanel, mainPanel;
        [SerializeField] private InputField loginId, loginPassword;
        [SerializeField] private InputField registerId, registerPassword, registerPasswordConfirmation, registerNickname;
        [SerializeField] private Text statusText, welcomeText;
        [SerializeField] private Button loginButton, googleButton, showRegisterButton, registerButton, showLoginButton, startButton, logoutButton;
        [SerializeField] private GameObject responsePopup;
        [SerializeField] private Text responsePopupText;
        [SerializeField] private Button responsePopupCloseButton;
        private IGoogleAuthProvider _google = new GoogleAuthProvider();
        private AuthView _currentView = AuthView.None;
        private bool _requestInProgress;
        private bool _logoutRequested;

        private enum AuthView { None, Login, Register, Main }

        private void Awake()
        {
            loginPassword.contentType = InputField.ContentType.Password;
            registerPassword.contentType = registerPasswordConfirmation.contentType = InputField.ContentType.Password;
            loginButton.onClick.AddListener(BeginLogin);
            registerButton.onClick.AddListener(BeginRegister);
            googleButton.onClick.AddListener(() => Run(GoogleLogin()));
            showRegisterButton.onClick.AddListener(() => NavigateTo(AuthView.Register));
            showLoginButton.onClick.AddListener(GoBack);
            startButton.onClick.AddListener(() => SceneManager.LoadScene(GameFlowUI.PlaySceneName));
            logoutButton.onClick.AddListener(BeginLogout);
            responsePopupCloseButton.onClick.AddListener(CloseResponsePopup);
            Show(AuthView.None);
            responsePopup.SetActive(false);
            SetStatus("로그인 정보를 확인하고 있습니다.");
        }

        private IEnumerator Start()
        {
            while (AuthManager.Instance == null)
                yield return null;

            auth = AuthManager.Instance;
            auth.StateChanged += HandleState;
            HandleState(auth.State, null);
        }

        private void OnDestroy() { if (auth != null) auth.StateChanged -= HandleState; }
        private void Run(IEnumerator operation) => StartCoroutine(operation);

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                GoBack();
        }

        private void BeginLogin()
        {
            string id = loginId.text;
            string password = loginPassword.text;
            ClearAllFields();
            _requestInProgress = true;
            Run(auth.Login(id, password));
        }

        private void BeginRegister()
        {
            string id = registerId.text;
            string password = registerPassword.text;
            string confirmation = registerPasswordConfirmation.text;
            string nickname = registerNickname.text;
            ClearAllFields();
            _requestInProgress = true;
            Run(auth.Register(id, password, confirmation, nickname));
        }

        private void BeginLogout()
        {
            ClearAllFields();
            _logoutRequested = true;
            _requestInProgress = true;
            Run(auth.Logout());
        }

        private IEnumerator GoogleLogin()
        {
            ClearAllFields();
            _requestInProgress = true;
            string token = null, error = null;
            yield return _google.SignIn((value, message) => { token = value; error = message; });
            if (string.IsNullOrWhiteSpace(token))
            {
                _requestInProgress = false;
                SetButtonsInteractable(true);
                SetStatus(error);
                ShowResponsePopup(error);
                yield break;
            }
            yield return auth.GoogleLogin(token, null);
        }

        private void HandleState(AuthState state, string message)
        {
            bool busy = state == AuthState.Busy || state == AuthState.Checking;
            SetButtonsInteractable(!busy);
            if (busy) SetStatus(state == AuthState.Checking
                ? "로그인 정보를 확인하고 있습니다."
                : "요청을 처리하고 있습니다.");
            if (!string.IsNullOrWhiteSpace(message))
            {
                SetStatus(message);
                ShowResponsePopup(message);
            }
            if (state == AuthState.SignedOut)
            {
                bool wasRequest = _requestInProgress;
                _requestInProgress = false;

                if (_currentView == AuthView.None || _logoutRequested || _currentView == AuthView.Main)
                {
                    _logoutRequested = false;
                    NavigateTo(AuthView.Login, false);
                }
                else if (!wasRequest && _currentView != AuthView.Register)
                {
                    NavigateTo(AuthView.Login, false);
                }
                // 로그인/회원가입 실패라면 현재 화면을 그대로 유지한다.
            }
            else if (state == AuthState.SignedIn)
            {
                _requestInProgress = false;
                welcomeText.text = $"{auth.Tokens.User?.nickname ?? "플레이어"}님, 환영합니다.";
                SetStatus("서버 인증에 성공했습니다.");
                Show(AuthView.Main);
                ShowResponsePopup("로그인이 완료되었습니다. 게임 시작 버튼을 눌러 게임을 시작할 수 있습니다.");
            }
        }

        private void GoBack()
        {
            if (_requestInProgress) return;
            if (_currentView == AuthView.Register) NavigateTo(AuthView.Login);
        }

        private void NavigateTo(AuthView view, bool clearStatus = true)
        {
            if (_requestInProgress) return;
            ClearAllFields();
            if (clearStatus) SetStatus(string.Empty);
            Show(view);
        }

        private void Show(AuthView view)
        {
            _currentView = view;
            loginPanel.SetActive(view == AuthView.Login);
            registerPanel.SetActive(view == AuthView.Register);
            mainPanel.SetActive(view == AuthView.Main);
        }

        private void ClearAllFields()
        {
            loginId.text = string.Empty;
            loginPassword.text = string.Empty;
            registerId.text = string.Empty;
            registerPassword.text = string.Empty;
            registerPasswordConfirmation.text = string.Empty;
            registerNickname.text = string.Empty;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            loginButton.interactable = interactable;
            registerButton.interactable = interactable;
            googleButton.interactable = interactable;
            showRegisterButton.interactable = interactable;
            showLoginButton.interactable = interactable;
            logoutButton.interactable = interactable;
        }

        private void ShowResponsePopup(string message)
        {
            if (responsePopup == null || responsePopupText == null ||
                string.IsNullOrWhiteSpace(message)) return;
            responsePopupText.text = message;
            responsePopup.SetActive(true);
            responsePopup.transform.SetAsLastSibling();
        }

        private void CloseResponsePopup()
        {
            if (responsePopup != null) responsePopup.SetActive(false);
        }

        private void SetStatus(string value) { if (statusText != null) statusText.text = value ?? string.Empty; }
    }
}
