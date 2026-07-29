using System;
using System.Collections.Generic;
using Sweeper.Input;
using UnityEngine;

namespace Sweeper.Gameplay.Ball
{
    public sealed class BallVolleyController : MonoBehaviour
    {
        [Header("Volley")]
        [SerializeField] private BallLauncher ballPrefab;
        [SerializeField, Min(1)] private int ballCount = 5;
        [SerializeField, Min(.05f)] private float ballSpacing = .55f;
        [SerializeField, Min(.05f)] private float ballDiameter = .42f;

        [Header("Speed")]
        [SerializeField, Min(.1f)] private float minimumSpeed = 8f;
        [SerializeField, Min(.1f)] private float maximumSpeed = 18f;

        private readonly List<BallLauncher> _balls = new();
        private SwipeLaunchInput _input;
        private Camera _camera;
        private Vector2 _launchPosition;
        private Vector2 _nextLaunchPosition;
        private int _returnedCount;
        private bool _isVolleyActive;
        private int _ballLayer = -1;
        private int _nextBallIndex;
        private Vector2 _scheduledDirection;
        private float _scheduledSpeed;
        private int _fixedStepsBetweenLaunches;
        private int _fixedStepsUntilNextLaunch;
        private int _pendingAdditionalBalls;
        private bool _isGameplayEnabled = true;
        private int _launchCount;

        public bool CanLaunch => _isGameplayEnabled && !_isVolleyActive;
        public IReadOnlyList<BallLauncher> Balls => _balls;
        public int LaunchCount => _launchCount;
        public int BallCount => ballCount;
        public float BallSpacing => ballSpacing;
        public Vector2 LaunchPosition => _launchPosition;
        public int PendingAdditionalBalls => _pendingAdditionalBalls;
        public event Action VolleyCompleted;

        public void SetGameplayEnabled(bool enabled)
        {
            _isGameplayEnabled = enabled;
        }

        public void QueueAdditionalBalls(int amount)
        {
            _pendingAdditionalBalls += Mathf.Max(0, amount);
        }

        public void Configure(SwipeLaunchInput input, Camera playCamera, Vector2 initialLaunchPosition)
        {
            if (_input != null)
                _input.SwipeReleased -= HandleSwipeReleased;

            _input = input;
            _camera = playCamera;
            _launchPosition = initialLaunchPosition;
            _nextLaunchPosition = initialLaunchPosition;
            _ballLayer = LayerMask.NameToLayer("Ball_Instance");
            if (_ballLayer >= 0)
                Physics2D.IgnoreLayerCollision(_ballLayer, _ballLayer, true);
            EnsureBallCount();

            if (_input != null)
                _input.SwipeReleased += HandleSwipeReleased;
        }

        private void OnDestroy()
        {
            if (_input != null)
                _input.SwipeReleased -= HandleSwipeReleased;

            ApplyToAllBalls(ball =>
            {
                if (ball != null)
                    ball.ReturnRequested -= HandleBallReturnRequested;
            });
        }

        private void HandleSwipeReleased(SwipeSnapshot swipe)
        {
            if (!CanLaunch)
                return;

            float speed = Mathf.Lerp(minimumSpeed, maximumSpeed, swipe.Strength);
            BeginVolley(swipe.Direction, speed);
        }

        private void BeginVolley(Vector2 direction, float speed)
        {
            _launchCount++;
            _isVolleyActive = true;
            _returnedCount = 0;
            _nextLaunchPosition = _launchPosition;
            EnsureBallCount();

            _scheduledDirection = direction.normalized;
            _scheduledSpeed = speed;
            float distancePerFixedStep =
                Mathf.Max(.01f, speed) * Time.fixedDeltaTime;
            _fixedStepsBetweenLaunches = Mathf.Max(
                1,
                Mathf.CeilToInt(ballSpacing / distancePerFixedStep));
            _fixedStepsUntilNextLaunch = 0;
            _nextBallIndex = 0;
        }

        private void FixedUpdate()
        {
            if (!_isVolleyActive || _nextBallIndex >= ballCount)
                return;

            if (_fixedStepsUntilNextLaunch > 0)
            {
                _fixedStepsUntilNextLaunch--;
                if (_fixedStepsUntilNextLaunch > 0)
                    return;
            }

            LaunchNextBall();
            _fixedStepsUntilNextLaunch = _fixedStepsBetweenLaunches;
        }

        private void LaunchNextBall()
        {
            _balls[_nextBallIndex].Launch(
                _launchPosition,
                _scheduledDirection,
                _scheduledSpeed);
            _nextBallIndex++;
        }

        private void HandleBallReturnRequested(BallLauncher ball, Vector2 returnPosition)
        {
            if (!_isVolleyActive)
                return;

            if (_returnedCount == 0)
                _nextLaunchPosition = returnPosition;

            ball.StopAt(returnPosition);
            _returnedCount++;

            if (_returnedCount < ballCount)
                return;

            _launchPosition = _nextLaunchPosition;
            if (_pendingAdditionalBalls > 0)
            {
                ballCount += _pendingAdditionalBalls;
                _pendingAdditionalBalls = 0;
                EnsureBallCount();
            }

            ResetAllBallsAt(_launchPosition);

            // CanLaunch becomes true only after every ball is reset.
            _isVolleyActive = false;
            VolleyCompleted?.Invoke();
        }

        private void EnsureBallCount()
        {
            ballCount = Mathf.Max(1, ballCount);

            while (_balls.Count < ballCount)
                _balls.Add(CreateBall(_balls.Count));

            for (int index = 0; index < _balls.Count; index++)
            {
                bool shouldBeActive = index < ballCount;
                _balls[index].gameObject.SetActive(shouldBeActive);
                if (shouldBeActive && !_balls[index].IsInFlight)
                    _balls[index].StopAt(_launchPosition);
            }
        }

        private void ResetAllBallsAt(Vector2 position)
        {
            ApplyToAllBalls(ball =>
            {
                if (ball == null)
                    return;

                ball.gameObject.SetActive(true);
                ball.StopAt(position);
            });
            Physics2D.SyncTransforms();
        }

        private void ApplyToAllBalls(Action<BallLauncher> operation)
        {
            for (int index = 0; index < _balls.Count; index++)
                operation(_balls[index]);
        }

        private BallLauncher CreateBall(int index)
        {
            if (ballPrefab == null)
            {
                UnityEngine.Debug.LogError("Ball prefab is not assigned.", this);
                return null;
            }

            BallLauncher ball = Instantiate(
                ballPrefab,
                _launchPosition,
                Quaternion.identity,
                transform);
            ball.name = $"Ball {index + 1:00}";
            if (_ballLayer >= 0)
                ball.gameObject.layer = _ballLayer;
            ball.Configure(_camera, _launchPosition, ballDiameter);
            ball.ReturnRequested += HandleBallReturnRequested;
            return ball;
        }
    }
}
