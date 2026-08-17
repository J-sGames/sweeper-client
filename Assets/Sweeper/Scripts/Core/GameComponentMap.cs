using Sweeper.Gameplay.Ball;
using Sweeper.UI;
using UnityEngine;

namespace Sweeper.Core
{
    public enum GameState
    {
        Playing,
        GameOver
    }

    public sealed class GameComponentMap : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private GameScore gameScore;
        [SerializeField] private BallVolleyController volleyController;

        [Header("UI")]
        [SerializeField] private ScoreUI scoreUI;
        [SerializeField] private GameOverController gameOverController;
        [SerializeField] private GameOverView gameOverView;

        public GameState State { get; private set; }
        public int Score => gameScore != null ? gameScore.Score : 0;

        public void Apply(GameState state)
        {
            AssignProperties();
            State = state;

            bool isPlaying = state == GameState.Playing;
            if (gameScore != null)
                gameScore.SetRunning(isPlaying);
            if (volleyController != null)
                volleyController.SetGameplayEnabled(isPlaying);

            if (isPlaying)
            {
                if (gameOverController != null)
                    gameOverController.ResetView();
                else
                    gameOverView?.Hide();
                return;
            }

            gameOverController?.Show(Score);
        }

        private void AssignProperties()
        {
            if (scoreUI != null)
                scoreUI.GameScore = gameScore;

            if (gameOverController == null)
                return;
        }
    }
}
