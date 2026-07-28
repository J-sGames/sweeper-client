using UnityEngine;

namespace Sweeper.Gameplay.Ball
{
    public sealed class BallCollisionAudioPool : MonoBehaviour
    {
        [SerializeField, Range(1, 32)] private int maximumVoices = 8;

        private static BallCollisionAudioPool _instance;
        private AudioSource[] _sources;
        private int _nextSource;

        private void Awake()
        {
            _instance = this;
            _sources = new AudioSource[Mathf.Max(1, maximumVoices)];
            for (int index = 0; index < _sources.Length; index++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                _sources[index] = source;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public static void Play(AudioClip clip, float volume, float pitch)
        {
            if (_instance == null || clip == null)
                return;

            AudioSource source = _instance._sources[_instance._nextSource];
            _instance._nextSource = (_instance._nextSource + 1) % _instance._sources.Length;
            source.Stop();
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }
    }
}
