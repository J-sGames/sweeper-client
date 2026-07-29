using UnityEngine;

namespace Sweeper.Gameplay.Board
{
    public sealed class RectangularPlayfield : MonoBehaviour
    {
        [Header("Fixed Playfield Ratio")]
        [SerializeField, Min(1f)] private float aspectWidth = 9f;
        [SerializeField, Min(1f)] private float aspectHeight = 16f;
        [SerializeField, Min(1f)] private float fieldHeight = 14.2f;
        [SerializeField] private Vector2 fieldCenter = Vector2.zero;

        [Header("Appearance")]
        [SerializeField] private float wallThickness = .18f;
        [SerializeField, Min(.2f)] private float returnZoneHeight = .9f;
        [SerializeField, Min(.05f)] private float launchClearance = .25f;
        [SerializeField] private Color fieldColor = new(.055f, .085f, .135f, 1f);
        [SerializeField] private Color wallColor = new(.22f, .45f, .62f, 1f);
        [SerializeField] private Color returnZoneColor = new(.12f, .75f, .7f, .18f);

        private static Sprite _whiteSprite;
        private Camera _fittedCamera;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        public float FieldWidth =>
            Mathf.Max(1f, fieldHeight) * Mathf.Max(1f, aspectWidth) / Mathf.Max(1f, aspectHeight);
        public Vector2 FieldSize => new(FieldWidth, Mathf.Max(1f, fieldHeight));
        public Vector2 InitialLaunchPosition =>
            new(
                fieldCenter.x,
                fieldCenter.y - FieldSize.y * .5f + returnZoneHeight + launchClearance);
        public float InnerLeft => fieldCenter.x - FieldWidth * .5f + wallThickness;
        public float InnerRight => fieldCenter.x + FieldWidth * .5f - wallThickness;
        public float InnerTop => fieldCenter.y + FieldSize.y * .5f - wallThickness;

        private void Start()
        {
            Build();
        }

        public void FitCamera(Camera playCamera)
        {
            if (playCamera == null)
                return;

            _fittedCamera = playCamera;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            playCamera.orthographic = true;
            float screenAspect = Mathf.Max(.01f, playCamera.aspect);
            float verticalSize = FieldSize.y * .5f;
            float horizontalSize = FieldSize.x / (2f * screenAspect);
            playCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
            playCamera.transform.position = new Vector3(fieldCenter.x, fieldCenter.y, -10f);
        }

        private void LateUpdate()
        {
            if (_fittedCamera == null ||
                (_lastScreenWidth == Screen.width &&
                 _lastScreenHeight == Screen.height))
                return;

            FitCamera(_fittedCamera);
        }

        private void Build()
        {
            if (transform.Find("Field") != null)
                return;

            Vector2 size = FieldSize;
            Vector2 center = fieldCenter;
            Vector2 min = center - size * .5f;
            Vector2 max = center + size * .5f;

            CreateVisual("Field", center, size, fieldColor, -20);
            CreateWall("Left Wall",
                new Vector2(min.x + wallThickness * .5f, center.y),
                new Vector2(wallThickness, size.y));
            CreateWall("Right Wall",
                new Vector2(max.x - wallThickness * .5f, center.y),
                new Vector2(wallThickness, size.y));
            CreateWall("Top Wall",
                new Vector2(center.x, max.y - wallThickness * .5f),
                new Vector2(size.x, wallThickness));

            GameObject returnZone = CreateVisual("Ball Return Zone",
                new Vector2(center.x, min.y + returnZoneHeight * .5f),
                new Vector2(size.x, returnZoneHeight),
                returnZoneColor,
                -4);
            returnZone.AddComponent<BoxCollider2D>();
            returnZone.AddComponent<BallReturnZone>().Configure(
                min.x + wallThickness,
                max.x - wallThickness,
                min.y + returnZoneHeight + launchClearance);
        }

        private void CreateWall(string objectName, Vector2 position, Vector2 size)
        {
            GameObject wall = CreateVisual(objectName, position, size, wallColor, -5);
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            PhysicsMaterial2D material = new($"{objectName} Bounce")
            {
                friction = 0f,
                bounciness = 1f
            };
            collider.sharedMaterial = material;
        }

        private GameObject CreateVisual(
            string objectName,
            Vector2 position,
            Vector2 size,
            Color color,
            int sortingOrder)
        {
            GameObject visual = new(objectName);
            visual.transform.SetParent(transform, false);
            visual.transform.position = position;
            visual.transform.localScale = size;

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return visual;
        }

        private void OnDrawGizmos()
        {
            Vector2 size = FieldSize;
            Vector2 center = fieldCenter;
            Vector2 min = center - size * .5f;
            Vector2 max = center + size * .5f;

            Gizmos.color = new Color(fieldColor.r, fieldColor.g, fieldColor.b, .35f);
            Gizmos.DrawCube(center, size);

            Gizmos.color = wallColor;
            Gizmos.DrawCube(
                new Vector2(min.x + wallThickness * .5f, center.y),
                new Vector2(wallThickness, size.y));
            Gizmos.DrawCube(
                new Vector2(max.x - wallThickness * .5f, center.y),
                new Vector2(wallThickness, size.y));
            Gizmos.DrawCube(
                new Vector2(center.x, max.y - wallThickness * .5f),
                new Vector2(size.x, wallThickness));

            Gizmos.color = returnZoneColor;
            Gizmos.DrawCube(
                new Vector2(center.x, min.y + returnZoneHeight * .5f),
                new Vector2(size.x, returnZoneHeight));

            Gizmos.color = new Color(.25f, 1f, .75f, .8f);
            Gizmos.DrawWireCube(center, size);
        }

        private static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null)
                    return _whiteSprite;

                Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
                texture.name = "Runtime White";
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * .5f, 1f);
                return _whiteSprite;
            }
        }
    }
}
