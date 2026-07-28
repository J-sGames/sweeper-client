using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Sweeper.Input
{
    /// <summary>
    /// Converts one primary pointer gesture into a launch request.
    /// Values are screen-independent so gameplay code does not depend on resolution.
    /// </summary>
    public sealed class SwipeLaunchInput : MonoBehaviour
    {
        [Header("Gesture")]
        [SerializeField, Range(0.005f, 0.1f)] private float minimumSwipe = 0.025f;
        [SerializeField, Range(0.1f, 0.8f)] private float maximumSwipe = 0.35f;
        [SerializeField, Range(0f, 0.8f)] private float minimumUpwardDirection = 0.15f;

        public event Action<SwipeSnapshot> SwipeStarted;
        public event Action<SwipeSnapshot> SwipeChanged;
        public event Action<SwipeSnapshot> SwipeReleased;
        public event Action SwipeCancelled;

        public bool IsDragging { get; private set; }
        public SwipeSnapshot Current { get; private set; }

        private int _activeTouchId = -1;
        private Vector2 _startPosition;

        private void Update()
        {
            if (ReadTouch())
                return;

            ReadMouse();
        }

        private bool ReadTouch()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
                return false;

            foreach (TouchControl touch in touchscreen.touches)
            {
                if (touch.press.wasPressedThisFrame && !IsDragging)
                {
                    Begin(touch.touchId.ReadValue(), touch.position.ReadValue());
                    return true;
                }

                if (!IsDragging || touch.touchId.ReadValue() != _activeTouchId)
                    continue;

                if (touch.press.wasReleasedThisFrame)
                    End(touch.position.ReadValue());
                else if (touch.press.isPressed)
                    Move(touch.position.ReadValue());
                else
                    Cancel();

                return true;
            }

            return IsDragging;
        }

        private void ReadMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
                Begin(0, position);
            else if (IsDragging && _activeTouchId == 0 && mouse.leftButton.wasReleasedThisFrame)
                End(position);
            else if (IsDragging && _activeTouchId == 0 && mouse.leftButton.isPressed)
                Move(position);
        }

        private void Begin(int pointerId, Vector2 position)
        {
            _activeTouchId = pointerId;
            _startPosition = position;
            IsDragging = true;
            Current = BuildSnapshot(position);
            SwipeStarted?.Invoke(Current);
        }

        private void Move(Vector2 position)
        {
            Current = BuildSnapshot(position);
            SwipeChanged?.Invoke(Current);
        }

        private void End(Vector2 position)
        {
            Current = BuildSnapshot(position);
            IsDragging = false;
            _activeTouchId = -1;

            if (Current.NormalizedDistance >= minimumSwipe &&
                Current.Direction.y >= minimumUpwardDirection)
                SwipeReleased?.Invoke(Current);
            else
                SwipeCancelled?.Invoke();
        }

        private void Cancel()
        {
            IsDragging = false;
            _activeTouchId = -1;
            Current = default;
            SwipeCancelled?.Invoke();
        }

        private SwipeSnapshot BuildSnapshot(Vector2 currentPosition)
        {
            float referenceSize = Mathf.Max(1f, Screen.height);
            Vector2 screenDelta = currentPosition - _startPosition;
            float normalizedDistance = screenDelta.magnitude / referenceSize;
            float strength = Mathf.InverseLerp(minimumSwipe, maximumSwipe, normalizedDistance);

            return new SwipeSnapshot(
                _startPosition,
                currentPosition,
                screenDelta.sqrMagnitude > 0f ? screenDelta.normalized : Vector2.zero,
                normalizedDistance,
                strength);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && IsDragging)
                Cancel();
        }
    }

    public readonly struct SwipeSnapshot
    {
        public readonly Vector2 StartScreenPosition;
        public readonly Vector2 CurrentScreenPosition;
        public readonly Vector2 Direction;
        public readonly float NormalizedDistance;
        public readonly float Strength;

        public SwipeSnapshot(
            Vector2 startScreenPosition,
            Vector2 currentScreenPosition,
            Vector2 direction,
            float normalizedDistance,
            float strength)
        {
            StartScreenPosition = startScreenPosition;
            CurrentScreenPosition = currentScreenPosition;
            Direction = direction;
            NormalizedDistance = normalizedDistance;
            Strength = strength;
        }
    }
}
