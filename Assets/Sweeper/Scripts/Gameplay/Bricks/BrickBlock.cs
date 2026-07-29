using Sweeper.Gameplay.Ball;
using Sweeper.Gameplay.CameraEffects;
using UnityEngine;

namespace Sweeper.Gameplay.Bricks
{
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class BrickBlock : MonoBehaviour
    {
        private static Sprite _previewSprite;
        private static Font _runtimeFont;
        private static AudioClip _generatedDestructionSound;

        [SerializeField] private Color color = new(.95f, .35f, .2f, 1f);
        [SerializeField, Min(1)] private int hitsToDestroy = 1;
        [SerializeField] private TextMesh hitCountText;

        [Header("Destruction Audio")]
        [SerializeField] private AudioClip destructionSound;
        [SerializeField, Range(0f, 1f)] private float destructionVolume = .75f;
        [SerializeField, Range(.5f, 2f)] private float destructionPitch = 1f;

        [Header("Impact Camera Shake")]
        [SerializeField, Min(0f)] private float impactShakeDuration = .06f;
        [SerializeField, Min(0f)] private float impactShakeIntensity = .025f;

        [Header("Destruction Camera Shake")]
        [SerializeField, Min(0f)] private float destructionShakeDuration = .16f;
        [SerializeField, Min(0f)] private float destructionShakeIntensity = .12f;

        [Header("Dynamic Shake Scaling")]
        [SerializeField, Min(.1f)] private float referenceBallSpeed = 12f;
        [SerializeField, Min(0f)] private float intensityGrowthPerCollision = .08f;
        [SerializeField, Min(1f)] private float maximumShakeMultiplier = 3f;

        private int _remainingHits;
        private bool _destroyRequested;

        public int HitsToDestroy => hitsToDestroy;
        public int RemainingHits => _remainingHits;

        public void Configure(Vector2 size, Color brickColor, int requiredHits)
        {
            color = brickColor;
            hitsToDestroy = Mathf.Max(1, requiredHits);
            _remainingHits = hitsToDestroy;
            _destroyRequested = false;
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
                !collision.collider.TryGetComponent(out BallLauncher ball))
                return;

            int collisionCount = ball.RegisterBrickCollision();
            float shakeMultiplier = CalculateShakeMultiplier(
                ball.Velocity.magnitude,
                collisionCount);
            _remainingHits--;
            if (_remainingHits > 0)
            {
                CameraShake.Play(
                    impactShakeDuration,
                    impactShakeIntensity * shakeMultiplier);
                RefreshVisual();
                return;
            }

            _destroyRequested = true;
            BallCollisionAudioPool.Play(
                destructionSound != null
                    ? destructionSound
                    : GeneratedDestructionSound,
                destructionVolume,
                destructionPitch);
            CameraShake.Play(
                destructionShakeDuration,
                destructionShakeIntensity * shakeMultiplier);
            Destroy(gameObject);
        }

        private float CalculateShakeMultiplier(float ballSpeed, int collisionCount)
        {
            float speedMultiplier = ballSpeed / Mathf.Max(.1f, referenceBallSpeed);
            float collisionMultiplier = 1f +
                Mathf.Max(0, collisionCount - 1) * intensityGrowthPerCollision;
            return Mathf.Clamp(
                speedMultiplier * collisionMultiplier,
                .1f,
                Mathf.Max(1f, maximumShakeMultiplier));
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

        private static AudioClip GeneratedDestructionSound
        {
            get
            {
                if (_generatedDestructionSound != null)
                    return _generatedDestructionSound;

                const int sampleRate = 44100;
                const float duration = .12f;
                int sampleCount = Mathf.CeilToInt(sampleRate * duration);
                float[] samples = new float[sampleCount];
                uint noiseState = 2463534242u;

                for (int index = 0; index < sampleCount; index++)
                {
                    float time = index / (float)sampleRate;
                    float progress = index / (float)sampleCount;
                    float envelope = Mathf.Pow(1f - progress, 3f);
                    float frequency = Mathf.Lerp(900f, 180f, progress);
                    float crack = Mathf.Sin(2f * Mathf.PI * frequency * time);

                    noiseState ^= noiseState << 13;
                    noiseState ^= noiseState >> 17;
                    noiseState ^= noiseState << 5;
                    float noise = (noiseState / (float)uint.MaxValue) * 2f - 1f;
                    samples[index] = (crack * .45f + noise * .55f) * envelope * .7f;
                }

                _generatedDestructionSound = AudioClip.Create(
                    "Generated Brick Destruction",
                    sampleCount,
                    1,
                    sampleRate,
                    false);
                _generatedDestructionSound.SetData(samples, 0);
                return _generatedDestructionSound;
            }
        }
    }
}
