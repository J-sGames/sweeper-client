using Sweeper.Input;
using Sweeper.Gameplay.Ball;
using Sweeper.Gameplay.Board;
using Sweeper.Gameplay.Bricks;
using Sweeper.UI;
using UnityEngine;

namespace Sweeper.Core
{
    /// <summary>Minimal composition root for the Play scene.</summary>
    public sealed class PlaySceneBootstrap : MonoBehaviour
    {
        [SerializeField] private SwipeLaunchInput swipeInput;

        private BrickRowSpawner _brickRowSpawner;
        [SerializeField] private GameComponentMap componentMap;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            if (swipeInput == null)
                swipeInput = GetComponent<SwipeLaunchInput>();
            if (componentMap == null)
                componentMap = GetComponent<GameComponentMap>();

            Camera playCamera = ConfigureCamera();
            RectangularPlayfield playfield = GetComponent<RectangularPlayfield>();
            if (playfield != null)
                playfield.FitCamera(playCamera);
            ConfigureVolley(playCamera, playfield);
            componentMap?.Apply(GameState.Playing);
            ConfigureGameFailureCallback();
        }

        private void OnDestroy()
        {
            if (_brickRowSpawner != null)
                _brickRowSpawner.GameFailed -= HandleGameFailed;
        }

        private static Camera ConfigureCamera()
        {
            Camera playCamera = Camera.main;
            if (playCamera == null)
            {
                GameObject cameraObject = new("Main Camera");
                playCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            playCamera.orthographic = true;
            playCamera.orthographicSize = 8f;
            playCamera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            playCamera.backgroundColor = new Color(.035f, .055f, .09f);

            if (playCamera.GetComponent<AudioListener>() == null)
                playCamera.gameObject.AddComponent<AudioListener>();

            if (FindFirstObjectByType<BallCollisionAudioPool>() == null)
                new GameObject("Ball Collision Audio Pool").AddComponent<BallCollisionAudioPool>();

            return playCamera;
        }

        private void ConfigureVolley(Camera playCamera, RectangularPlayfield playfield)
        {
            Vector2 initialPosition = playfield != null
                ? playfield.InitialLaunchPosition
                : Vector2.zero;
            BallVolleyController volley = GetComponent<BallVolleyController>();
            if (volley == null)
                volley = gameObject.AddComponent<BallVolleyController>();
            volley.Configure(swipeInput, playCamera, initialPosition);
        }

        private void ConfigureGameFailureCallback()
        {
            _brickRowSpawner = GetComponent<BrickRowSpawner>();
            if (_brickRowSpawner != null)
                _brickRowSpawner.GameFailed += HandleGameFailed;
        }

        private void HandleGameFailed()
        {
            Debug.Log(
                $"Game failed: a living brick crossed the game-over line. " +
                $"Spawned bricks: {_brickRowSpawner.TotalSpawnedBrickCount}, " +
                $"bricks remaining in scene: {_brickRowSpawner.CurrentSceneBrickCount}.",
                this);
            if (componentMap != null)
                componentMap.Apply(GameState.GameOver);
            else
                Debug.LogError("GameComponentMap is not assigned.", this);
        }
    }
}
