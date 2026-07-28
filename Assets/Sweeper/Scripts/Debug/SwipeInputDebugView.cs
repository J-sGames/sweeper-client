using Sweeper.Input;
using UnityEngine;

namespace Sweeper.Debugging
{
    public sealed class SwipeInputDebugView : MonoBehaviour
    {
        [SerializeField] private SwipeLaunchInput input;
        [SerializeField] private bool showInReleaseBuild;

        private string _state = "IDLE";
        private SwipeSnapshot _snapshot;
        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;

        private bool IsVisible => UnityEngine.Debug.isDebugBuild || Application.isEditor || showInReleaseBuild;

        private void Awake()
        {
            if (input == null)
                input = GetComponent<SwipeLaunchInput>();
        }

        private void OnEnable()
        {
            if (input == null)
                return;

            input.SwipeStarted += HandleStarted;
            input.SwipeChanged += HandleChanged;
            input.SwipeReleased += HandleReleased;
            input.SwipeCancelled += HandleCancelled;
        }

        private void OnDisable()
        {
            if (input == null)
                return;

            input.SwipeStarted -= HandleStarted;
            input.SwipeChanged -= HandleChanged;
            input.SwipeReleased -= HandleReleased;
            input.SwipeCancelled -= HandleCancelled;
        }

        private void HandleStarted(SwipeSnapshot snapshot)
        {
            _state = "DRAG START";
            _snapshot = snapshot;
        }

        private void HandleChanged(SwipeSnapshot snapshot)
        {
            _state = "DRAGGING";
            _snapshot = snapshot;
        }

        private void HandleReleased(SwipeSnapshot snapshot)
        {
            _state = "RELEASED / LAUNCH";
            _snapshot = snapshot;
        }

        private void HandleCancelled()
        {
            _state = "CANCELLED";
            _snapshot = default;
        }

        private void OnGUI()
        {
            if (!IsVisible || input == null || !input.IsDragging)
                return;

            BuildStyles();
            DrawGesture();
            DrawPanel();
        }

        private void DrawGesture()
        {
            if (!input.IsDragging && _state != "RELEASED / LAUNCH")
                return;

            Vector2 start = ScreenToGui(_snapshot.StartScreenPosition);
            Vector2 current = ScreenToGui(_snapshot.CurrentScreenPosition);
            DrawLine(start, current, new Color(.25f, 1f, .75f, .95f), 4f);
            DrawRect(new Rect(start.x - 7f, start.y - 7f, 14f, 14f), Color.white);
            DrawRect(new Rect(current.x - 9f, current.y - 9f, 18f, 18f),
                new Color(.25f, 1f, .75f));
        }

        private void DrawPanel()
        {
            float width = Mathf.Min(430f, Screen.width - 24f);
            Rect panel = new(12f, 12f, width, 190f);
            DrawRect(panel, new Color(.015f, .025f, .045f, .88f));

            GUI.Label(new Rect(26f, 22f, width - 28f, 30f), $"INPUT  {_state}", _titleStyle);
            GUI.Label(new Rect(26f, 56f, width - 28f, 24f),
                $"Start    {_snapshot.StartScreenPosition.x:0}, {_snapshot.StartScreenPosition.y:0}", _labelStyle);
            GUI.Label(new Rect(26f, 80f, width - 28f, 24f),
                $"Current  {_snapshot.CurrentScreenPosition.x:0}, {_snapshot.CurrentScreenPosition.y:0}", _labelStyle);
            GUI.Label(new Rect(26f, 104f, width - 28f, 24f),
                $"Direction  {_snapshot.Direction.x:0.00}, {_snapshot.Direction.y:0.00}", _labelStyle);
            GUI.Label(new Rect(26f, 128f, width - 28f, 24f),
                $"Distance   {_snapshot.NormalizedDistance:0.000}", _labelStyle);
            GUI.Label(new Rect(26f, 152f, width - 28f, 24f),
                $"Strength   {_snapshot.Strength:P0}", _labelStyle);

            Rect gauge = new(150f, 158f, width - 176f, 10f);
            DrawRect(gauge, new Color(1f, 1f, 1f, .15f));
            DrawRect(new Rect(gauge.x, gauge.y, gauge.width * _snapshot.Strength, gauge.height),
                new Color(.25f, 1f, .75f));
        }

        private void BuildStyles()
        {
            if (_labelStyle != null)
                return;

            int fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.width / 28f, 13f, 22f));
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = new Color(.82f, .9f, .96f) }
            };
            _titleStyle = new GUIStyle(_labelStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.25f, 1f, .75f) }
            };
        }

        private static Vector2 ScreenToGui(Vector2 point) => new(point.x, Screen.height - point.y);

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            float angle = Vector2.SignedAngle(Vector2.right, end - start);
            float length = Vector2.Distance(start, end);

            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width * .5f, length, width), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }
    }
}
