using Sweeper.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sweeper.UI
{
    public sealed class ScoreUI : MonoBehaviour
    {
        [SerializeField] private Text scoreText;
        [SerializeField] private GameScore gameScore;

        public GameScore GameScore
        {
            get => gameScore;
            set
            {
                if (gameScore == value)
                    return;

                if (isActiveAndEnabled && gameScore != null)
                    gameScore.ScoreChanged -= Refresh;
                gameScore = value;
                if (!isActiveAndEnabled || gameScore == null)
                    return;

                gameScore.ScoreChanged += Refresh;
                Refresh(gameScore.Score);
            }
        }

        private void OnEnable()
        {
            if (gameScore == null)
                return;

            gameScore.ScoreChanged += Refresh;
            Refresh(gameScore.Score);
        }

        private void OnDisable()
        {
            if (gameScore != null)
                gameScore.ScoreChanged -= Refresh;
        }

        private void Refresh(int score)
        {
            if (scoreText != null)
                scoreText.text = $"SCORE  {score:N0}";
        }
    }
}
