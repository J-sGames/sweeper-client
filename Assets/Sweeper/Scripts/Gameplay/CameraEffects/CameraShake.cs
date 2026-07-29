using UnityEngine;

namespace Sweeper.Gameplay.CameraEffects
{
    [DefaultExecutionOrder(1000)]
    public sealed class CameraShake : MonoBehaviour
    {
        private Vector3 _lastOffset;
        private float _remainingTime;
        private float _duration;
        private float _intensity;
        private float _noiseSeed;

        public static void Play(float duration, float intensity)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            CameraShake shake = camera.GetComponent<CameraShake>();
            if (shake == null)
                shake = camera.gameObject.AddComponent<CameraShake>();
            shake.RequestShake(duration, intensity);
        }

        private void Awake()
        {
            _noiseSeed = Random.Range(0f, 1000f);
        }

        private void RequestShake(float duration, float intensity)
        {
            _duration = Mathf.Max(_duration, Mathf.Max(.01f, duration));
            _remainingTime = Mathf.Max(_remainingTime, duration);
            _intensity = Mathf.Max(_intensity, Mathf.Max(0f, intensity));
        }

        private void LateUpdate()
        {
            transform.localPosition -= _lastOffset;
            _lastOffset = Vector3.zero;

            if (_remainingTime <= 0f)
            {
                _duration = 0f;
                _intensity = 0f;
                return;
            }

            _remainingTime = Mathf.Max(0f, _remainingTime - Time.unscaledDeltaTime);
            float strength = _duration > 0f
                ? _remainingTime / _duration
                : 0f;
            float sampleTime = Time.unscaledTime * 35f;
            _lastOffset = new Vector3(
                Mathf.PerlinNoise(_noiseSeed, sampleTime) * 2f - 1f,
                Mathf.PerlinNoise(_noiseSeed + 100f, sampleTime) * 2f - 1f,
                0f) * (_intensity * strength);
            transform.localPosition += _lastOffset;
        }

        private void OnDisable()
        {
            transform.localPosition -= _lastOffset;
            _lastOffset = Vector3.zero;
        }
    }
}
