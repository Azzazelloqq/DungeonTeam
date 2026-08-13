using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonTeam.Gameplay.Skills.Editor
{
    internal static class SkillVfxPreviewSceneBuilder
    {
        internal const string ScenePath =
            "Assets/Scenes/Development/SkillVfxPreview.unity";
        internal const string SourceSlotName = "SourceSlot";
        internal const string TargetSlotName = "TargetSlot";

        [MenuItem("DungeonTeam/Skills/VFX Lab/Rebuild Preview Scene")]
        private static void CreateFromMenu()
        {
            CreateSceneAsset();
            EditorUtility.DisplayDialog(
                "Skill VFX Lab",
                $"Preview scene created at:\n{ScenePath}",
                "OK");
        }

        [MenuItem("DungeonTeam/Skills/VFX Lab/Open Preview Scene")]
        private static void OpenFromMenu()
        {
            OpenSceneAndLab();
        }

        internal static void OpenSceneAndLab()
        {
            if (!System.IO.File.Exists(ScenePath))
                CreateSceneAsset();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SkillVfxPreviewWindow.OpenAndBindScene();
        }

        internal static void CreateSceneAsset()
        {
            EnsureFolder("Assets/Scenes", "Development");

            var previousScene = SceneManager.GetActiveScene();
            var previewScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            previewScene.name = "SkillVfxPreview";
            SceneManager.SetActiveScene(previewScene);

            try
            {
                BuildEnvironment();
                var sourceSlot = new GameObject(SourceSlotName);
                sourceSlot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var targetSlot = new GameObject(TargetSlotName);
                targetSlot.transform.SetPositionAndRotation(
                    new Vector3(0f, 0f, 5f),
                    Quaternion.Euler(0f, 180f, 0f));

                Selection.activeGameObject = sourceSlot;
                EditorSceneManager.MarkSceneDirty(previewScene);
                if (!EditorSceneManager.SaveScene(previewScene, ScenePath))
                    throw new System.InvalidOperationException(
                        $"Failed to save preview scene at '{ScenePath}'.");
            }
            finally
            {
                EditorSceneManager.CloseScene(previewScene, removeScene: true);
                if (previousScene.IsValid() && previousScene.isLoaded)
                    SceneManager.SetActiveScene(previousScene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Skill VFX preview scene created: {ScenePath}");
        }

        private static void BuildEnvironment()
        {
            var environment = new GameObject("PreviewEnvironment");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_10m";
            ground.transform.SetParent(environment.transform);
            ground.transform.position = new Vector3(0f, 0f, 2.5f);
            ground.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            Object.DestroyImmediate(ground.GetComponent<Collider>());

            var reference = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reference.name = "ReferenceCube_1m";
            reference.transform.SetParent(environment.transform);
            reference.transform.position = new Vector3(-2.5f, 0.5f, 2.5f);
            Object.DestroyImmediate(reference.GetComponent<Collider>());

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cameraObject = new GameObject("Preview Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 50f;
            cameraObject.transform.position = new Vector3(7f, 5f, -6f);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 1f, 2.5f) - cameraObject.transform.position);
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
