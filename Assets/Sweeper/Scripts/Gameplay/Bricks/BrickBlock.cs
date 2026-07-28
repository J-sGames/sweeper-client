using Sweeper.Gameplay.Ball;
using UnityEngine;

namespace Sweeper.Gameplay.Bricks
{
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class BrickBlock : MonoBehaviour
    {
        private static Sprite _previewSprite;
        private static Font _runtimeFont;

        [SerializeField] private Color color = new(.95f, .35f, .2f, 1f);
        [SerializeField, Min(1)] private int hitsToDestroy = 1;
        [SerializeField] private TextMesh hitCountText;

        private int _remainingHits;
        private bool _destroyRequested;

        public int HitsToDestroy => hitsToDestroy;
        public int RemainingHits => _remainingHits;

        public void Configure(Vector2 size, Color brickColor)
        {
            color = brickColor;
            transform.localScale = new Vector3(size.x, size.y, 1f);
            RefreshVisual();
        }

        private void Awake()
        {
            _remainingHits = Mathf.Max(1, hitsToDestroy);
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                _remainingHits = Mathf.Max(1, hitsToDestroy);
                _destroyRequested = false;
                RefreshVisual();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_destroyRequested ||
                !collision.collider.TryGetComponent(out BallLauncher _))
                return;

            _remainingHits--;
            if (_remainingHits > 0)
            {
                RefreshVisual();
                return;
            }

            _destroyRequested = true;
            Destroy(gameObject);
        }

        private void RefreshVisual()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
                return;

            renderer.sprite = PreviewSprite;
            float healthRatio = Application.isPlaying && hitsToDestroy > 0
                ? Mathf.Clamp01(_remainingHits / (float)hitsToDestroy)
                : 1f;
            renderer.color = Color.Lerp(
                new Color(color.r, color.g, color.b, .45f),
                color,
                healthRatio);
            renderer.sortingOrder = 5;

            if (hitCountText == null)
                hitCountText = GetComponentInChildren<TextMesh>(true);
            if (hitCountText == null)
                return;

            hitCountText.text = (Application.isPlaying
                ? Mathf.Max(0, _remainingHits)
                : Mathf.Max(1, hitsToDestroy)).ToString();
            if (_runtimeFont == null)
                _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_runtimeFont != null)
                hitCountText.font = _runtimeFont;
            hitCountText.color = Color.white;
            hitCountText.anchor = TextAnchor.MiddleCenter;
            hitCountText.alignment = TextAlignment.Center;
            hitCountText.fontSize = 48;
            hitCountText.characterSize = .1f;
            hitCountText.transform.localPosition = new Vector3(0f, 0f, -.02f);
            hitCountText.transform.localScale = new Vector3(
                1f / Mathf.Max(.001f, Mathf.Abs(transform.localScale.x)),
                1f / Mathf.Max(.001f, Mathf.Abs(transform.localScale.y)),
                1f);
            MeshRenderer textRenderer = hitCountText.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                if (_runtimeFont != null)
                    textRenderer.sharedMaterial = _runtimeFont.material;
                textRenderer.sortingOrder = 6;
            }
        }

        private static Sprite PreviewSprite
        {
            get
            {
                if (_previewSprite != null)
                    return _previewSprite;

                Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "Brick Preview",
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _previewSprite = Sprite.Create(
                    texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * .5f, 1f);
                _previewSprite.name = "Brick Preview Sprite";
                _previewSprite.hideFlags = HideFlags.HideAndDontSave;
                return _previewSprite;
            }
        }
    }
}
