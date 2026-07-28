#if UNITY_EDITOR
using Sweeper.Core;
using Sweeper.Gameplay.Bricks;
using UnityEditor;
using UnityEngine;

namespace Sweeper.Editor
{
    [CustomEditor(typeof(PlaySceneBootstrap))]
    internal sealed class PlaySceneBootstrapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Temporary Brick Tools", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (!GUILayout.Button("벽돌 한 줄 추가", GUILayout.Height(40f)))
                    return;

                PlaySceneBootstrap bootstrap = (PlaySceneBootstrap)target;
                BrickRowSpawner spawner = bootstrap.GetComponent<BrickRowSpawner>();
                if (spawner == null)
                    spawner = Undo.AddComponent<BrickRowSpawner>(bootstrap.gameObject);

                GameObject row = spawner.AddRow();
                if (row == null)
                    return;

                Undo.RegisterCreatedObjectUndo(row, "Add Brick Row");
                Selection.activeGameObject = row;
                EditorUtility.SetDirty(bootstrap.gameObject);
            }

            if (EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Play Mode를 종료하면 벽돌 생성 버튼을 사용할 수 있습니다.", MessageType.Info);
        }
    }
}
#endif
