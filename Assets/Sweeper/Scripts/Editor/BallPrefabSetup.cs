#if UNITY_EDITOR
using Sweeper.Gameplay.Ball;
using UnityEditor;
using UnityEngine;

namespace Sweeper.Editor
{
    [InitializeOnLoad]
    internal static class BallPrefabSetup
    {
        private const string PrefabPath = "Assets/Sweeper/Prefabs/Balls/Ball.prefab";

        static BallPrefabSetup()
        {
            EditorApplication.delayCall += EnsurePrefabExists;
        }

        [MenuItem("Sweeper/Setup/Rebuild Ball Prefab")]
        public static void RebuildPrefab()
        {
            AssetDatabase.DeleteAsset(PrefabPath);
            EnsurePrefabExists();
        }

        private static void EnsurePrefabExists()
        {
            if (AssetDatabase.LoadAssetAtPath<BallLauncher>(PrefabPath) != null)
                return;

            GameObject ballObject = new("Ball");
            ballObject.AddComponent<SpriteRenderer>();
            ballObject.AddComponent<CircleCollider2D>();
            Rigidbody2D body = ballObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.freezeRotation = true;
            ballObject.AddComponent<BallLauncher>();

            PrefabUtility.SaveAsPrefabAsset(ballObject, PrefabPath);
            Object.DestroyImmediate(ballObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
