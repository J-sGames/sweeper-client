#if UNITY_EDITOR
using Sweeper.Gameplay.Bricks;
using Sweeper.Gameplay.Board;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sweeper.Editor
{
    [InitializeOnLoad]
    internal static class BrickRowSpawnerSceneSetup
    {
        private const string PlayScenePath = "Assets/Sweeper/Scenes/Play.unity";

        static BrickRowSpawnerSceneSetup()
        {
            EditorApplication.delayCall += EnsureSpawnerInActiveScene;
            EditorSceneManager.sceneOpened += HandleSceneOpened;
        }

        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == PlayScenePath)
                EditorApplication.delayCall += EnsureSpawnerInActiveScene;
        }

        [MenuItem("Sweeper/Bricks/Ensure Brick Row Spawner")]
        public static void EnsureSpawnerInActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != PlayScenePath)
                return;

            RectangularPlayfield playfield = Object.FindFirstObjectByType<RectangularPlayfield>();
            if (playfield == null)
            {
                Debug.LogError("Rectangular Playfield was not found in the Play scene.");
                return;
            }

            BrickRowSpawner spawner = playfield.GetComponent<BrickRowSpawner>();
            if (spawner == null)
            {
                spawner = Undo.AddComponent<BrickRowSpawner>(playfield.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("Brick Row Spawner was added to the Play Scene object.", spawner);
            }

            Selection.activeObject = spawner.gameObject;
            EditorGUIUtility.PingObject(spawner.gameObject);
        }
    }

    [CustomEditor(typeof(BrickRowSpawner))]
    internal sealed class BrickRowSpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty prefabProperty = serializedObject.FindProperty("brickPrefab");
            if (prefabProperty.objectReferenceValue == null)
            {
                prefabProperty.objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<BrickBlock>(
                        "Assets/Sweeper/Prefabs/Bricks/Brick.prefab");
                serializedObject.ApplyModifiedProperties();
            }

            DrawDefaultInspector();
            EditorGUILayout.Space(10f);

            BrickRowSpawner spawner = (BrickRowSpawner)target;
            int count = spawner.CalculateBlockCount();
            EditorGUILayout.HelpBox(
                count > 0
                    ? $"한 줄에 {count}개가 생성되며 실제 폭은 " +
                      $"{spawner.CalculateFittedBlockWidth():0.###}입니다."
                    : "Rectangular Playfield를 연결해주세요.",
                count > 0 ? MessageType.Info : MessageType.Warning);

            using (new EditorGUI.DisabledScope(count <= 0))
            {
                if (GUILayout.Button("벽돌 한 줄 추가", GUILayout.Height(36f)))
                {
                    GameObject row = spawner.AddRow();
                    if (row != null)
                    {
                        Undo.RegisterCreatedObjectUndo(row, "Add Brick Row");
                        Selection.activeGameObject = row;
                        EditorUtility.SetDirty(spawner);
                    }
                }
            }
        }
    }
}
#endif
