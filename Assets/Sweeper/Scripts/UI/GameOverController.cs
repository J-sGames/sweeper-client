using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sweeper.UI
{
    public sealed class GameOverController : MonoBehaviour
    {
        [SerializeField] private GameOverView view;

        private bool _isVisible;
        private DateTime _startedTime;

        private void Awake()
        {
            _startedTime = DateTime.UtcNow;
        }

        public GameOverView View
        {
            get => view;
            set => view = value;
        }

        public void Show(int score)
        {
            if (view == null)
                return;

            if (_isVisible && view.gameObject.activeSelf)
                return;

            _isVisible = true;

            view.Configure(
                () => SceneManager.LoadScene(GameFlowUI.PlaySceneName),
                () => SceneManager.LoadScene(GameFlowUI.MainSceneName));
            view.Show(score, _startedTime);

        }

        public void ResetView()
        {
            _isVisible = false;
            view?.Hide();
        }
    }
}
