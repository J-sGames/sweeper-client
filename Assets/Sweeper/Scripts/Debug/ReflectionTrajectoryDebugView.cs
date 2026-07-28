using System.Collections.Generic;
using Sweeper.Gameplay.Ball;
using Sweeper.Input;
using UnityEngine;

namespace Sweeper.Debugging
{
    public sealed class ReflectionTrajectoryDebugView : MonoBehaviour
    {
        [SerializeField] private SwipeLaunchInput input;
        [SerializeField, Range(1, 20)] private int maximumReflections = 12;
        [SerializeField, Min(1f)] private float maximumSegmentDistance = 30f;
        [SerializeField, Min(.01f)] private float lineWidth = .055f;
        [SerializeField] private bool showInReleaseBuild;

        private static readonly Color[] Rainbow =
        {
            new(1f, .18f, .15f),      // red
            new(1f, .48f, .08f),      // orange
            new(1f, .88f, .08f),      // yellow
            new(.15f, .9f, .28f),     // green
            new(.12f, .55f, 1f),      // blue
            new(.22f, .18f, .7f),     // indigo
            new(.62f, .2f, .92f)      // violet
        };

        private readonly List<LineRenderer> _segments = new();
        private BallLauncher _ball;
        private Collider2D _ballCollider;
        private Material _lineMaterial;

        private bool IsVisible => UnityEngine.Debug.isDebugBuild || Application.isEditor || showInReleaseBuild;

        private void Awake()
        {
            if (input == null)
                input = GetComponent<SwipeLaunchInput>();

            _ball = FindFirstObjectByType<BallLauncher>();
            if (_ball != null)
                _ballCollider = _ball.GetComponent<Collider2D>();

            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
            EnsureSegmentCount();
            HideAll();
        }

        private void Update()
        {
            if (!IsVisible || input == null || !input.IsDragging || input.Current.Direction.y <= 0f)
            {
                HideAll();
                return;
            }

            if (_ball == null)
            {
                _ball = FindFirstObjectByType<BallLauncher>();
                if (_ball != null)
                    _ballCollider = _ball.GetComponent<Collider2D>();
            }

            if (_ball != null)
                DrawTrajectory(_ball.LaunchWorldPosition, input.Current.Direction.normalized);
        }

        private void DrawTrajectory(Vector2 origin, Vector2 direction)
        {
            EnsureSegmentCount();
            int segmentIndex = 0;

            while (segmentIndex <= maximumReflections)
            {
                RaycastHit2D hit = FindFirstSolidHit(origin, direction);
                Vector2 end = hit.collider == null
                    ? origin + direction * maximumSegmentDistance
                    : hit.centroid;

                LineRenderer segment = _segments[segmentIndex];
                segment.enabled = true;
                segment.positionCount = 2;
                segment.SetPosition(0, origin);
                segment.SetPosition(1, end);
                Color color = Rainbow[segmentIndex % Rainbow.Length];
                segment.startColor = color;
                segment.endColor = new Color(color.r, color.g, color.b, .72f);

                segmentIndex++;
                if (hit.collider == null || segmentIndex > maximumReflections)
                    break;

                direction = Vector2.Reflect(direction, hit.normal).normalized;
                origin = hit.centroid + direction * .015f;
            }

            for (int index = segmentIndex; index < _segments.Count; index++)
                _segments[index].enabled = false;
        }

        private RaycastHit2D FindFirstSolidHit(Vector2 origin, Vector2 direction)
        {
            float radius = _ballCollider == null
                ? 0f
                : Mathf.Abs(_ballCollider.transform.lossyScale.x) * .5f;
            foreach (RaycastHit2D hit in Physics2D.CircleCastAll(
                         origin, radius, direction, maximumSegmentDistance))
            {
                if (hit.collider != null && hit.collider != _ballCollider && !hit.collider.isTrigger)
                    return hit;
            }

            return default;
        }

        private void EnsureSegmentCount()
        {
            int required = maximumReflections + 1;
            while (_segments.Count < required)
            {
                GameObject segmentObject = new($"Reflection {_segments.Count:00}");
                segmentObject.transform.SetParent(transform, false);
                LineRenderer renderer = segmentObject.AddComponent<LineRenderer>();
                renderer.material = _lineMaterial;
                renderer.useWorldSpace = true;
                renderer.startWidth = lineWidth;
                renderer.endWidth = lineWidth;
                renderer.numCapVertices = 4;
                renderer.sortingOrder = 50;
                _segments.Add(renderer);
            }
        }

        private void HideAll()
        {
            foreach (LineRenderer segment in _segments)
                segment.enabled = false;
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null)
                Destroy(_lineMaterial);
        }
    }
}
