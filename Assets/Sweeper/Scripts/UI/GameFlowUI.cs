using UnityEngine;
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

        [SerializeField] private InputField playerNameInput;
        [SerializeField] private Button startButton;

        private void Awake()
        {
            if (playerNameInput == null || startButton == null)
            {
                Debug.LogError("Main menu UI references are not configured.", this);
                enabled = false;
                return;
            }

            playerNameInput.text = PlayerPrefs.GetString(
                PlayerNamePrefsKey,
                DefaultPlayerName);
            startButton.onClick.AddListener(StartGame);
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(StartGame);
        }

        private void StartGame()
        {
            SavePlayerName(playerNameInput.text);
            SceneManager.LoadScene(PlaySceneName);
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

    }

}
