using System.Collections.Generic;
using Sweeper.Gameplay.Ball;
using UnityEngine;
using Action = System.Action;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sweeper.Gameplay.Bricks
{
    public sealed class BrickRowSpawner : MonoBehaviour
    {
        [SerializeField] private Sweeper.Gameplay.Board.RectangularPlayfield playfield;
        [SerializeField] private BrickBlock brickPrefab;
        [SerializeField] private BallCountPickup ballCountPickupPrefab;

        [Header("Block Size")]
        [SerializeField, Min(.1f)] private float blockWidth = 1.05f;
        [SerializeField, Min(.1f)] private float blockHeight = .72f;
        [SerializeField, Min(0f)] private float horizontalGap = .08f;

        [Header("Placement")]
        [SerializeField, Range(1, 12)] private int previewRowCount = 3;
        [SerializeField, Min(1)] private int startingBricksPerRow = 2;
        [SerializeField, Min(1)] private int launchesPerBlockIncrease = 50;
        [SerializeField] private Color blockColor = new(.95f, .35f, .2f, 1f);
        [SerializeField] private bool spawnOnStart = true;

        [Header("Game Failure")]
        [SerializeField, Min(1)] private int maximumBrickRowCount = 8;
        [SerializeField, Min(.01f)] private float gameOverLineWidth = .06f;
        [SerializeField] private Color gameOverLineColor = new(1f, .2f, .2f, .9f);

        [Header("Ball Reward Placement")]
        [SerializeField, Range(1, 50)] private int rewardPlacementAttempts = 24;
        [SerializeField, Min(0f)] private float rewardBottomClearance = 1.2f;

        private BallVolleyController _volley;
        private int _generation;
        private int _totalSpawnedBrickCount;
        private bool _isGameFailed;

        public int CurrentGeneration => _generation;
        public int MaximumBrickRowCount
        {
            get
            {
                int boardLimit = CalculateBoardMaximumBrickRowCount();
                return boardLimit > 0
                    ? Mathf.Min(maximumBrickRowCount, boardLimit)
                    : maximumBrickRowCount;
            }
        }
        public int CurrentBrickRowCount
        {
            get
            {
                Transform rowsRoot = transform.Find("Brick Rows");
                if (rowsRoot == null)
                    return 0;

                BrickBlock[] bricks = rowsRoot.GetComponentsInChildren<BrickBlock>();
                HashSet<Transform> occupiedRows = new();
                for (int index = 0; index < bricks.Length; index++)
                    occupiedRows.Add(bricks[index].transform.parent);
                return occupiedRows.Count;
            }
        }
        public int TotalSpawnedBrickCount => _totalSpawnedBrickCount;
        public int CurrentSceneBrickCount
        {
            get
            {
                Transform rowsRoot = transform.Find("Brick Rows");
                return rowsRoot != null
                    ? rowsRoot.GetComponentsInChildren<BrickBlock>().Length
                    : 0;
            }
        }
        public bool IsGameFailed => _isGameFailed;
        public int LaunchCount => _volley != null ? _volley.LaunchCount : 0;
        public int StartingBricksPerRow => startingBricksPerRow;
        public int LaunchesPerBlockIncrease => launchesPerBlockIncrease;
        public int CurrentBricksPerRow => CalculateBricksPerRow(LaunchCount);
        public event Action GameFailed;

        private void Start()
        {
            _volley = GetComponent<BallVolleyController>();
            if (_volley != null)
            {
                _volley.VolleyCompleted += HandleVolleyCompleted;
                if (_isGameFailed)
                    _volley.SetGameplayEnabled(false);
            }

            BuildGameOverLine();

            if (spawnOnStart)
                AddRow();
        }

        private void OnDestroy()
        {
            if (_volley != null)
                _volley.VolleyCompleted -= HandleVolleyCompleted;
        }

        private void HandleVolleyCompleted()
        {
            AddRow();
        }

        public void SetMaximumBrickRowCount(int maximumRowCount)
        {
            maximumBrickRowCount = Mathf.Max(1, maximumRowCount);
            BuildGameOverLine();
            EvaluateGameFailure();
        }

        public int CalculateBoardMaximumBrickRowCount()
        {
            if (playfield == null)
                return 0;

            float availableHeight =
                playfield.InnerTop - playfield.InitialLaunchPosition.y;
            int totalSlots = Mathf.FloorToInt(availableHeight / Mathf.Max(.1f, blockHeight));

            // One empty slot at the top and two empty slots at the bottom.
            return Mathf.Max(1, totalSlots - 3);
        }

        public float CalculateGameOverLineY()
        {
            if (playfield == null)
                return 0f;

            return playfield.InnerTop - (MaximumBrickRowCount + 1) * blockHeight;
        }

        public int CalculateBlockCount()
        {
            if (playfield == null)
                return 0;

            float usableWidth = playfield.InnerRight - playfield.InnerLeft;
            return Mathf.Max(1, Mathf.CeilToInt(
                (usableWidth + horizontalGap) /
                (Mathf.Max(.1f, blockWidth) + horizontalGap)));
        }

        public float CalculateFittedBlockWidth()
        {
            int count = CalculateBlockCount();
            if (count <= 0 || playfield == null)
                return 0f;

            float usableWidth = playfield.InnerRight - playfield.InnerLeft;
            return (usableWidth - horizontalGap * (count - 1)) / count;
        }

        public int CalculateBricksPerRow(int launchCount)
        {
            int maximum = CalculateBlockCount();
            if (maximum <= 0)
                return 0;

            int increase = Mathf.Max(0, launchCount) /
                Mathf.Max(1, launchesPerBlockIncrease);
            return Mathf.Min(maximum, Mathf.Max(1, startingBricksPerRow) + increase);
        }

        public void SetBrickCountProgression(
            int startingBrickCount,
            int launchInterval)
        {
            startingBricksPerRow = Mathf.Max(1, startingBrickCount);
            launchesPerBlockIncrease = Mathf.Max(1, launchInterval);
        }

        public GameObject AddRow()
        {
            if (_isGameFailed)
                return null;

            if (playfield == null)
            {
                Debug.LogError("Rectangular Playfield is not assigned.", this);
                return null;
            }
            if (brickPrefab == null)
            {
                Debug.LogError("Brick prefab is not assigned.", this);
                return null;
            }

            Transform rowsRoot = GetOrCreateRowsRoot();
            ShiftExistingRowsDown(rowsRoot);
            _generation++;
            GameObject row = new($"Brick Row {_generation:00} (HP {_generation})");
            row.transform.SetParent(rowsRoot, false);

            int count = CalculateBlockCount();
            float fittedWidth = CalculateFittedBlockWidth();
            float startX = playfield.InnerLeft + fittedWidth * .5f;
            // The first grid slot below the top wall always remains empty.
            float y = playfield.InnerTop - blockHeight * 1.5f;
            bool[] occupied = BuildRandomOccupancy(
                count,
                CalculateBricksPerRow(LaunchCount));

            for (int index = 0; index < count; index++)
            {
                if (!occupied[index])
                    continue;

                Vector3 position = new(
                    startX + index * (fittedWidth + horizontalGap), y, 0f);
                BrickBlock brick = CreateBrick(row.transform);
                brick.name = $"Brick {index + 1:00}";
                brick.transform.position = position;
                brick.Configure(
                    new Vector2(fittedWidth, blockHeight),
                    blockColor,
                    _generation);
                _totalSpawnedBrickCount++;
            }

            TrySpawnPickup(row.transform, fittedWidth);
            EvaluateGameFailure();
            return row;
        }

        private void EvaluateGameFailure()
        {
            if (_isGameFailed || !HasLivingBrickCrossedGameOverLine())
                return;

            _isGameFailed = true;
            if (_volley != null)
                _volley.SetGameplayEnabled(false);
            GameFailed?.Invoke();
        }

        private bool HasLivingBrickCrossedGameOverLine()
        {
            Transform rowsRoot = transform.Find("Brick Rows");
            if (rowsRoot == null)
                return false;

            float gameOverLineY = CalculateGameOverLineY();
            BrickBlock[] bricks = rowsRoot.GetComponentsInChildren<BrickBlock>();
            for (int index = 0; index < bricks.Length; index++)
            {
                float brickBottom = bricks[index].transform.position.y -
                    Mathf.Abs(bricks[index].transform.lossyScale.y) * .5f;
                if (brickBottom < gameOverLineY - .001f)
                    return true;
            }
            return false;
        }

        private static bool[] BuildRandomOccupancy(int count, int brickCount)
        {
            bool[] occupied = new bool[count];
            int required = Mathf.Clamp(brickCount, 0, count);
            for (int occupiedCount = 0; occupiedCount < required;)
            {
                int index = Random.Range(0, count);
                if (occupied[index])
                    continue;
                occupied[index] = true;
                occupiedCount++;
            }

            return occupied;
        }

        private void TrySpawnPickup(Transform parent, float fittedBlockWidth)
        {
            if (ballCountPickupPrefab == null)
                return;

            float diameter = Mathf.Min(fittedBlockWidth, blockHeight) * .62f;
            float radius = diameter * .5f;
            float minimumX = playfield.InnerLeft + radius;
            float maximumX = playfield.InnerRight - radius;
            float minimumY = playfield.InitialLaunchPosition.y + rewardBottomClearance + radius;
            float maximumY = playfield.InnerTop - blockHeight - radius;

            Physics2D.SyncTransforms();
            for (int attempt = 0; attempt < rewardPlacementAttempts; attempt++)
            {
                Vector2 position = new(
                    Random.Range(minimumX, maximumX),
                    Random.Range(minimumY, maximumY));
                if (Physics2D.OverlapCircle(position, radius * 1.15f) != null)
                    continue;

                BallCountPickup pickup = CreatePickup(parent);
                pickup.name = "Ball Reward +1";
                pickup.transform.position = position;
                pickup.Configure(diameter);
                return;
            }

            Debug.LogWarning("No empty position was found for the ball reward.", this);
        }

        private void ShiftExistingRowsDown(Transform rowsRoot)
        {
            Vector3 offset = Vector3.down * blockHeight;
            for (int index = 0; index < rowsRoot.childCount; index++)
                rowsRoot.GetChild(index).localPosition += offset;
        }

        private void BuildGameOverLine()
        {
            if (playfield == null || !Application.isPlaying)
                return;

            const string lineName = "Game Over Line";
            Transform existing = transform.Find(lineName);
            LineRenderer line = existing != null
                ? existing.GetComponent<LineRenderer>()
                : null;
            if (line == null)
            {
                GameObject lineObject = new(lineName);
                lineObject.transform.SetParent(transform, false);
                line = lineObject.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.numCapVertices = 2;
                line.sortingOrder = 10;
            }

            float y = CalculateGameOverLineY();
            line.SetPosition(0, new Vector3(playfield.InnerLeft, y, 0f));
            line.SetPosition(1, new Vector3(playfield.InnerRight, y, 0f));
            line.startWidth = gameOverLineWidth;
            line.endWidth = gameOverLineWidth;
            line.startColor = gameOverLineColor;
            line.endColor = gameOverLineColor;
        }

        private Transform GetOrCreateRowsRoot()
        {
            Transform existing = transform.Find("Brick Rows");
            if (existing != null)
                return existing;

            GameObject root = new("Brick Rows");
            root.transform.SetParent(transform, false);
            return root.transform;
        }

        private BrickBlock CreateBrick(Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    brickPrefab.gameObject, parent.gameObject.scene);
                instance.transform.SetParent(parent, false);
                return instance.GetComponent<BrickBlock>();
            }
#endif
            return Instantiate(brickPrefab, parent);
        }

        private BallCountPickup CreatePickup(Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    ballCountPickupPrefab.gameObject, parent.gameObject.scene);
                instance.transform.SetParent(parent, false);
                return instance.GetComponent<BallCountPickup>();
            }
#endif
            return Instantiate(ballCountPickupPrefab, parent);
        }

        private void Reset()
        {
            playfield = GetComponent<Sweeper.Gameplay.Board.RectangularPlayfield>();
        }

        private void OnValidate()
        {
            maximumBrickRowCount = Mathf.Max(1, maximumBrickRowCount);
            gameOverLineWidth = Mathf.Max(.01f, gameOverLineWidth);
            startingBricksPerRow = Mathf.Max(1, startingBricksPerRow);
            launchesPerBlockIncrease = Mathf.Max(1, launchesPerBlockIncrease);
        }

        private void OnDrawGizmos()
        {
            if (playfield == null)
                return;

            int count = CalculateBlockCount();
            float fittedWidth = CalculateFittedBlockWidth();
            if (count <= 0 || fittedWidth <= 0f)
                return;

            float startX = playfield.InnerLeft + fittedWidth * .5f;
            Color fill = new(blockColor.r, blockColor.g, blockColor.b, .28f);
            Color outline = new(blockColor.r, blockColor.g, blockColor.b, .9f);
            int previewBrickCount = CalculateBricksPerRow(LaunchCount);

            for (int row = 0; row < previewRowCount; row++)
            {
                float y = playfield.InnerTop - blockHeight * 1.5f
                    - row * blockHeight;
                bool[] occupied = BuildPreviewOccupancy(count, previewBrickCount, row);
                for (int column = 0; column < count; column++)
                {
                    if (!occupied[column])
                        continue;

                    Vector3 center = new(
                        startX + column * (fittedWidth + horizontalGap), y, 0f);
                    Vector3 size = new(fittedWidth, blockHeight, .02f);
                    Gizmos.color = fill;
                    Gizmos.DrawCube(center, size);
                    Gizmos.color = outline;
                    Gizmos.DrawWireCube(center, size);
                }
            }

            Gizmos.color = gameOverLineColor;
            float gameOverY = CalculateGameOverLineY();
            Gizmos.DrawLine(
                new Vector3(playfield.InnerLeft, gameOverY, 0f),
                new Vector3(playfield.InnerRight, gameOverY, 0f));
        }

        private static bool[] BuildPreviewOccupancy(
            int slotCount,
            int brickCount,
            int row)
        {
            bool[] occupied = new bool[slotCount];
            int required = Mathf.Clamp(brickCount, 0, slotCount);
            int start = slotCount > 0 ? row % slotCount : 0;
            for (int index = 0; index < required; index++)
                occupied[(start + index) % slotCount] = true;
            return occupied;
        }
    }
}
