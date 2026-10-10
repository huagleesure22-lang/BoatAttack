using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tidebound.Editor
{
    public static class TideSceneTools
    {
        const string ScenePath = "Assets/scenes/tidebound_Island.unity";

        [MenuItem("Tidebound/Open Island Adventure")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        // Recreate from the original scene only on an explicit editor command. Source assets are preserved.
        [MenuItem("Tidebound/Rebuild Adventure From Original Island")]
        public static void Rebuild()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("Rebuild Tidebound", "Replace the adventure scene with a fresh island conversion? Original BoatAttack scenes and prefabs are preserved.", "Rebuild", "Cancel")) return;
            var scene = EditorSceneManager.OpenScene("Assets/scenes/static_Island.unity");
            foreach (var root in scene.GetRootGameObjects())
                if (PrefabUtility.IsPartOfPrefabInstance(root))
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            foreach (var t in transforms)
                if (t != null && (t.name == "Logic" || t.name == "Dynamic Objects" || t.name == "Props_Floating"))
                    Object.DestroyImmediate(t.gameObject);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var camera in root.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(camera.gameObject);
            new GameObject("Tidebound Adventure").AddComponent<TideAdventure>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }

        [MenuItem("Tidebound/Build Adventure (Current Desktop Target)")]
        public static void Build()
        {
            string folder = EditorUtility.OpenFolderPanel("Choose player output folder", "Builds", "");
            if (string.IsNullOrEmpty(folder)) return;
            var target = EditorUserBuildSettings.activeBuildTarget;
            string filename;
            if (target == BuildTarget.StandaloneWindows64) filename = "Tidebound.exe";
            else if (target == BuildTarget.StandaloneOSX) filename = "Tidebound.app";
            else if (target == BuildTarget.StandaloneLinux64) filename = "Tidebound.x86_64";
            else { Debug.LogError("Select a desktop build target first. Tidebound currently supports keyboard and mouse."); return; }
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = System.IO.Path.Combine(folder, filename),
                target = target,
                options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.LogError("Tidebound build failed: " + report.summary.result);
        }
    }
}
