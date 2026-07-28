using Sweeper.Gameplay.Ball;
using UnityEngine;
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
        [SerializeField, Min(0f)] private float verticalGap = .1f;

        [Header("Placement")]
        [SerializeField, Min(0f)] private float topPadding = .65f;
        [SerializeField, Range(1, 12)] private int previewRowCount = 3;
        [SerializeField, Range(0f, 1f)] private float brickFillProbability = .7f;
        [SerializeField, Min(1)] private int minimumBricksPerRow = 2;
        [SerializeField] private Color blockColor = new(.95f, .35f, .2f, 1f);
        [SerializeField] private bool spawnOnStart = true;

        [Header("Ball Reward Placement")]
        [SerializeField, Range(1, 50)] private int rewardPlacementAttempts = 24;
        [SerializeField, Min(0f)] private float rewardBottomClearance = 1.2f;

        private BallVolleyController _volley;
        private int _generation;

        public int CurrentGeneration => _generation;

        private void Start()
        {
            _volley = GetComponent<BallVolleyController>();
            if (_volley != null)
                _volley.VolleyCompleted += HandleVolleyCompleted;

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

        public GameObject AddRow()
        {
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
            float y = playfield.InnerTop - topPadding - blockHeight * .5f;
            bool[] occupied = BuildRandomOccupancy(count);

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
            }

            TrySpawnPickup(row.transform, fittedWidth);
            return row;
        }

        private bool[] BuildRandomOccupancy(int count)
        {
            bool[] occupied = new bool[count];
            int occupiedCount = 0;
            for (int index = 0; index < count; index++)
            {
                occupied[index] = Random.value <= brickFillProbability;
                if (occupied[index])
                    occupiedCount++;
            }

            int required = Mathf.Min(count, Mathf.Max(1, minimumBricksPerRow));
            while (occupiedCount < required)
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
            float maximumY = playfield.InnerTop - topPadding - radius;

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
            Vector3 offset = Vector3.down * (blockHeight + verticalGap);
            for (int index = 0; index < rowsRoot.childCount; index++)
                rowsRoot.GetChild(index).localPosition += offset;
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

            for (int row = 0; row < previewRowCount; row++)
            {
                float y = playfield.InnerTop - topPadding - blockHeight * .5f
                    - row * (blockHeight + verticalGap);
                for (int column = 0; column < count; column++)
                {
                    // Stable pseudo-random preview; runtime placement remains truly random.
                    uint hash = (uint)((row + 1) * 73856093 ^ (column + 1) * 19349663);
                    float sample = (hash % 1000) / 999f;
                    if (sample > brickFillProbability)
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
        }
    }
}
