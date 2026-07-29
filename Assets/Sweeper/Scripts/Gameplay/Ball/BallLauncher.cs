using System;
using UnityEngine;

namespace Sweeper.Gameplay.Ball
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class BallLauncher : MonoBehaviour
    {
        private static Sprite _circleSprite;
        private static AudioClip _collisionClip;

        private Camera _camera;
        private Rigidbody2D _body;
        private CircleCollider2D _collider;
        private Vector2 _launchWorldPosition;
        private float _lastCollisionSoundTime = float.NegativeInfinity;

        [Header("Collision Sound")]
        [SerializeField] private AudioClip collisionSound;
        [SerializeField, Range(0f, 1f)] private float collisionVolume = .45f;
        [SerializeField, Min(0f)] private float fullVolumeImpactSpeed = 10f;
        [SerializeField, Min(0f)] private float soundCooldown = .035f;
        [SerializeField, Range(0f, .3f)] private float pitchVariation = .08f;

        [Header("Trajectory Safety")]
        [SerializeField, Range(.05f, .5f)] private float minimumVerticalSpeedRatio = .15f;

        private float _flightSpeed;

        public event Action<BallLauncher, Vector2> ReturnRequested;

        public Vector2 LaunchWorldPosition => _launchWorldPosition;
        public bool IsInFlight { get; private set; }
        public Vector2 Velocity => _body == null ? Vector2.zero : _body.linearVelocity;

        public void Configure(Camera playCamera, Vector2 initialLaunchPosition, float ballDiameter)
        {
            _camera = playCamera;
            _launchWorldPosition = initialLaunchPosition;
            transform.localScale = Vector3.one * ballDiameter;
            StopAt(initialLaunchPosition);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.linearDamping = 0f;
            _body.angularDamping = 0f;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.freezeRotation = true;

            _collider = GetComponent<CircleCollider2D>();
            _collider.radius = .5f;
            _collider.sharedMaterial = new PhysicsMaterial2D("Ball Bounce")
            {
                friction = 0f,
                bounciness = 1f
            };

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = CircleSprite;
            renderer.color = new Color(.25f, .95f, 1f);
            renderer.sortingOrder = 10;

        }

        private void FixedUpdate()
        {
            if (!IsInFlight || _camera == null)
                return;

            CorrectNearHorizontalVelocity();

            Vector3 viewport = _camera.WorldToViewportPoint(_body.position);
            if (viewport.x < -.1f || viewport.x > 1.1f || viewport.y < -.1f || viewport.y > 1.1f)
                RequestReturn(_body.position);
        }

        public void Launch(Vector2 origin, Vector2 direction, float speed)
        {
            _launchWorldPosition = origin;
            enabled = true;
            _body.simulated = true;
            _body.position = origin;
            _body.linearVelocity = direction.normalized * speed;
            _flightSpeed = Mathf.Max(.01f, speed);
            _collider.enabled = true;
            IsInFlight = true;
        }

        private void CorrectNearHorizontalVelocity()
        {
            Vector2 velocity = _body.linearVelocity;
            float speed = Mathf.Max(_flightSpeed, velocity.magnitude);
            if (speed <= .01f ||
                Mathf.Abs(velocity.y) >= speed * minimumVerticalSpeedRatio)
                return;

            float verticalSign = Mathf.Abs(velocity.y) > .001f
                ? Mathf.Sign(velocity.y)
                : 1f;
            float horizontalSign = Mathf.Abs(velocity.x) > .001f
                ? Mathf.Sign(velocity.x)
                : 1f;
            float verticalSpeed = speed * minimumVerticalSpeedRatio;
            float horizontalSpeed = Mathf.Sqrt(
                Mathf.Max(0f, speed * speed - verticalSpeed * verticalSpeed));
            _body.linearVelocity = new Vector2(
                horizontalSign * horizontalSpeed,
                verticalSign * verticalSpeed);
        }

        public void EnterReturnZone(float x, float y)
        {
            if (IsInFlight)
                RequestReturn(new Vector2(x, y));
        }

        public void StopAt(Vector2 position)
        {
            _launchWorldPosition = position;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.position = position;
            _collider.enabled = false;
            _body.simulated = false;
            IsInFlight = false;
            enabled = false;
        }

        private void RequestReturn(Vector2 position)
        {
            IsInFlight = false;
            _body.linearVelocity = Vector2.zero;
            _collider.enabled = false;
            _body.simulated = false;
            ReturnRequested?.Invoke(this, position);
            enabled = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsInFlight || Time.unscaledTime - _lastCollisionSoundTime < soundCooldown)
                return;

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < .05f)
                return;

            float impactStrength = Mathf.Clamp01(
                impactSpeed / Mathf.Max(.01f, fullVolumeImpactSpeed));
            AudioClip clip = collisionSound != null ? collisionSound : GeneratedCollisionClip;
            BallCollisionAudioPool.Play(
                clip,
                collisionVolume * Mathf.Lerp(.3f, 1f, impactStrength),
                1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation));
            _lastCollisionSoundTime = Time.unscaledTime;
        }

        private static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite != null)
                    return _circleSprite;

                const int size = 64;
                Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
                texture.name = "Runtime Ball";
                texture.filterMode = FilterMode.Bilinear;
                Vector2 center = Vector2.one * ((size - 1) * .5f);
                float radius = size * .48f;

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
                return _circleSprite;
            }
        }

        private static AudioClip GeneratedCollisionClip
        {
            get
            {
                if (_collisionClip != null)
                    return _collisionClip;

                const int sampleRate = 44100;
                const float duration = .055f;
                int sampleCount = Mathf.CeilToInt(sampleRate * duration);
                float[] samples = new float[sampleCount];

                for (int index = 0; index < sampleCount; index++)
                {
                    float time = index / (float)sampleRate;
                    float progress = index / (float)sampleCount;
                    float envelope = Mathf.Pow(1f - progress, 4f);
                    float tone = Mathf.Sin(2f * Mathf.PI * 1150f * time);
                    float overtone = Mathf.Sin(2f * Mathf.PI * 2300f * time) * .28f;
                    samples[index] = (tone + overtone) * envelope * .6f;
                }

                _collisionClip = AudioClip.Create(
                    "Ball Collision", sampleCount, 1, sampleRate, false);
                _collisionClip.SetData(samples, 0);
                return _collisionClip;
            }
        }
    }
}
