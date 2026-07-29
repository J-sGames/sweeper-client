using Sweeper.Input;
using UnityEngine;

namespace Sweeper.Gameplay.Ball
{
    public sealed class AimDirectionArrow : MonoBehaviour
    {
        [SerializeField] private SwipeLaunchInput input;
        [SerializeField] private BallVolleyController volley;

        [Header("Arrow Appearance")]
        [SerializeField] private Sprite arrowSprite;
        [SerializeField, Min(0f)] private float distanceFromLaunchPosition = .75f;
        [SerializeField, Min(.01f)] private float arrowScale = 1f;
        [SerializeField] private float spriteAngleOffset;
        [SerializeField] private Color arrowColor = new(1f, 1f, 1f, .9f);
        [SerializeField] private Color invalidDirectionColor = new(1f, .15f, .15f, .95f);

        private SpriteRenderer _renderer;

        private void Awake()
        {
            if (input == null)
                input = GetComponent<SwipeLaunchInput>();
            if (volley == null)
                volley = GetComponent<BallVolleyController>();

            GameObject arrowObject = new("Aim Direction Arrow");
            arrowObject.transform.SetParent(transform, false);
            _renderer = arrowObject.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = 60;
            _renderer.enabled = false;
        }

        private void Update()
        {
            if (_renderer == null ||
                arrowSprite == null ||
                input == null ||
                volley == null ||
                !volley.CanLaunch ||
                !input.IsDragging ||
                input.Current.Direction.sqrMagnitude <= .0001f)
            {
                if (_renderer != null)
                    _renderer.enabled = false;
                return;
            }

            Vector2 direction = input.Current.Direction.normalized;
            Transform arrow = _renderer.transform;
            arrow.position = volley.LaunchPosition +
                direction * distanceFromLaunchPosition;
            arrow.rotation = Quaternion.Euler(
                0f,
                0f,
                Vector2.SignedAngle(Vector2.up, direction) + spriteAngleOffset);
            arrow.localScale = Vector3.one * arrowScale;

            _renderer.sprite = arrowSprite;
            _renderer.color = input.IsDirectionLaunchable(direction)
                ? arrowColor
                : invalidDirectionColor;
            _renderer.enabled = true;
        }
    }
}
