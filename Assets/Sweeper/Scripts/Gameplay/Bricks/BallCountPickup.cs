using Sweeper.Gameplay.Ball;
using UnityEngine;

namespace Sweeper.Gameplay.Bricks
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public sealed class BallCountPickup : MonoBehaviour
    {
        private static Sprite _circleSprite;

        [SerializeField, Min(1)] private int additionalBalls = 1;
        [SerializeField] private Color color = new(.25f, 1f, .55f, 1f);

        [Header("Collection Audio")]
        [SerializeField] private AudioClip collectionSound;
        [SerializeField, Range(0f, 1f)] private float collectionVolume = .8f;
        [SerializeField, Range(.5f, 2f)] private float collectionPitch = 1.25f;

        private BallVolleyController _volley;
        private bool _collected;

        public void Configure(float diameter)
        {
            transform.localScale = Vector3.one * diameter;
            RefreshVisual();
        }

        private void Awake()
        {
            _volley = FindFirstObjectByType<BallVolleyController>();
            CircleCollider2D trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = .5f;
            RefreshVisual();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || !other.TryGetComponent(out BallLauncher ball) || !ball.IsInFlight)
                return;

            if (_volley == null)
                _volley = FindFirstObjectByType<BallVolleyController>();
            if (_volley == null)
                return;

            _collected = true;
            _volley.QueueAdditionalBalls(additionalBalls);
            BallCollisionAudioPool.Play(
                collectionSound,
                collectionVolume,
                collectionPitch);
            Destroy(gameObject);
        }

        private void RefreshVisual()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = CircleSprite;
            renderer.color = color;
            renderer.sortingOrder = 7;
        }

        private static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite != null)
                    return _circleSprite;

                const int size = 48;
                Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
                texture.name = "Ball Reward";
                texture.hideFlags = HideFlags.HideAndDontSave;
                Vector2 center = Vector2.one * ((size - 1) * .5f);
                float radius = size * .46f;

                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float alpha = Mathf.Clamp01(
                        radius + .75f - Vector2.Distance(new Vector2(x, y), center));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }

                texture.Apply();
                _circleSprite = Sprite.Create(
                    texture, new Rect(0f, 0f, size, size), Vector2.one * .5f, size);
                _circleSprite.hideFlags = HideFlags.HideAndDontSave;
                return _circleSprite;
            }
        }
    }
}
