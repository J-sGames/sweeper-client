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

        [Header("Block Size")]
        [SerializeField, Min(.1f)] private float blockWidth = 1.05f;
        [SerializeField, Min(.1f)] private float blockHeight = .72f;
        [SerializeField, Min(0f)] private float horizontalGap = .08f;
        [SerializeField, Min(0f)] private float verticalGap = .1f;

        [Header("Placement")]
        [SerializeField, Min(0f)] private float topPadding = .65f;
        [SerializeField, Range(1, 12)] private int previewRowCount = 3;
        [SerializeField] private Color blockColor = new(.95f, .35f, .2f, 1f);

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
            int rowIndex = rowsRoot.childCount;
            GameObject row = new($"Brick Row {rowIndex + 1:00}");
            row.transform.SetParent(rowsRoot, false);

            int count = CalculateBlockCount();
            float fittedWidth = CalculateFittedBlockWidth();
            float startX = playfield.InnerLeft + fittedWidth * .5f;
            float y = playfield.InnerTop - topPadding - blockHeight * .5f
                - rowIndex * (blockHeight + verticalGap);

            for (int index = 0; index < count; index++)
            {
                BrickBlock brick = CreateBrick(row.transform);
                brick.name = $"Brick {index + 1:00}";
                brick.transform.position = new Vector3(
                    startX + index * (fittedWidth + horizontalGap), y, 0f);
                brick.Configure(new Vector2(fittedWidth, blockHeight), blockColor);
            }

            return row;
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
