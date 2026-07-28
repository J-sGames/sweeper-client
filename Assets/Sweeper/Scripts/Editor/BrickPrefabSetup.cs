#if UNITY_EDITOR
using Sweeper.Gameplay.Bricks;
using UnityEditor;
using UnityEngine;

namespace Sweeper.Editor
{
    [InitializeOnLoad]
    internal static class BrickPrefabSetup
    {
        private const string PrefabPath = "Assets/Sweeper/Prefabs/Bricks/Brick.prefab";

        static BrickPrefabSetup()
        {
            EditorApplication.delayCall += EnsurePrefabExists;
        }

        [MenuItem("Sweeper/Setup/Rebuild Brick Prefab")]
        public static void RebuildPrefab()
        {
            AssetDatabase.DeleteAsset(PrefabPath);
            EnsurePrefabExists();
        }

        private static void EnsurePrefabExists()
        {
            if (AssetDatabase.LoadAssetAtPath<BrickBlock>(PrefabPath) != null)
                return;

            GameObject brickObject = new("Brick");
            SpriteRenderer renderer = brickObject.AddComponent<SpriteRenderer>();
            brickObject.AddComponent<BoxCollider2D>();
            brickObject.AddComponent<BrickBlock>();
            GameObject textObject = new("Hit Count");
            textObject.transform.SetParent(brickObject.transform, false);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = "1";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.characterSize = .1f;
            renderer.sprite = null;

            PrefabUtility.SaveAsPrefabAsset(brickObject, PrefabPath);
            Object.DestroyImmediate(brickObject);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
