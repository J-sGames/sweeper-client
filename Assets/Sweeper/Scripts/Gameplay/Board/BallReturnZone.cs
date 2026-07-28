using Sweeper.Gameplay.Ball;
using UnityEngine;

namespace Sweeper.Gameplay.Board
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BallReturnZone : MonoBehaviour
    {
        private float _minimumX;
        private float _maximumX;
        private float _launchY;

        public void Configure(float minimumX, float maximumX, float launchY)
        {
            _minimumX = minimumX;
            _maximumX = maximumX;
            _launchY = launchY;
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out BallLauncher ball))
                return;

            // The launch point is close to this zone. Only a descending ball is returning.
            if (!ball.IsInFlight || ball.Velocity.y >= -0.01f)
                return;

            ball.EnterReturnZone(
                Mathf.Clamp(ball.transform.position.x, _minimumX, _maximumX),
                _launchY);
        }
    }
}
