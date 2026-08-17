using System;
using UnityEngine;

namespace Sweeper.Core
{
    public sealed class GameScore : MonoBehaviour
    {
        public const int MinimumScore = 0;
        public const int BrickHitScore = 1;
        public const int BrickDestroyedScore = 100;

        private const float ScoreDecayInterval = 1f;

        private float _decayTimer;
        private bool _isRunning = true;

        public static GameScore Instance { get; private set; }
        public int Score { get; private set; }
        public event Action<int> ScoreChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Score = MinimumScore;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            _decayTimer += Time.deltaTime;
            while (_decayTimer >= ScoreDecayInterval)
            {
                _decayTimer -= ScoreDecayInterval;
                AddScore(-1);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RegisterBrickHit()
        {
            AddScore(BrickHitScore);
        }

        public void RegisterBrickDestroyed()
        {
            AddScore(BrickDestroyedScore);
        }

        public void Stop()
        {
            SetRunning(false);
        }

        public void SetRunning(bool isRunning)
        {
            _isRunning = isRunning;
        }

        private void AddScore(int amount)
        {
            int nextScore = Mathf.Max(MinimumScore, Score + amount);
            if (nextScore == Score)
                return;

            Score = nextScore;
            ScoreChanged?.Invoke(Score);
        }
    }
}
