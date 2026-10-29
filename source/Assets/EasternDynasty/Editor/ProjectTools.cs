using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EasternDynasty.Editor
{
    [InitializeOnLoad]
    public static class ProjectTools
    {
        private const string ScenePath = "Assets/Scenes/EasternDynasty.unity";
        static ProjectTools() { EditorApplication.delayCall += EnsureProject; }

        private static void EnsureProject()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath)) Prepare();
        }

        [MenuItem("Eastern Dynasty/Prepare scene")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            PlayerSettings.companyName = "EasternDynasty";
            PlayerSettings.productName = "EasternDynasty";
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("EASTERN_DYNASTY_PROJECT_READY");
        }

        [MenuItem("Eastern Dynasty/Play prototype")]
        public static void Play()
        {
            Prepare();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            { EditorSceneManager.OpenScene(ScenePath); EditorApplication.isPlaying = true; }
        }

        [MenuItem("Eastern Dynasty/Build Windows")]
        public static void BuildWindows()
        {
            Prepare();
            AssetCatalogTools.ValidateForBuild();
            foreach(string path in AssetDatabase.FindAssets("t:Shader",new[]{"Assets/EasternDynasty/Resources"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var shader=AssetDatabase.LoadAssetAtPath<Shader>(path);
                if(ShaderUtil.ShaderHasError(shader))throw new Exception("Shader has compile errors: "+path);
            }
            Directory.CreateDirectory("Builds/Windows");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/EasternDynasty.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + result.summary.result);
            Debug.Log("EASTERN_DYNASTY_WINDOWS_BUILD_OK");
        }

        public static void VerifySerialization()
        {
            Prepare();
            foreach(bool youzhou in new[]{false,true})
            {
            var original = youzhou ? World.CreateYouzhou(0,"Serialization test") : World.Create(Homeland.Han, 4, "Serialization test");
            original.Tick(300);
            CheckRoundTrip(original, "idle");
            original.March(original.lands.First(t => original.AttackProblem(t.id).Length == 0).id);
            original.Tick(original.order.duration / 2);
            CheckRoundTrip(original, "outbound");
            original.Tick(original.order.remaining + .1);
            CheckRoundTrip(original, "returning");
            original.Tick(original.order.remaining);
            CheckRoundTrip(original, "returned");
            }
            Debug.Log("EASTERN_DYNASTY_SERIALIZATION_OK: legacy and Youzhou: idle, outbound, returning, returned");
        }

        [MenuItem("Eastern Dynasty/Validate architecture and assets")]
        public static void VerifyArchitecture()
        {
            Prepare();
            AssetCatalogTools.ValidateForBuild();
            RepositoryChecks.Run();
            Debug.Log("EASTERN_DYNASTY_ARCHITECTURE_OK");
        }

        private static void CheckRoundTrip(World original, string state)
        {
            string json = JsonUtility.ToJson(original);
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/serialization-" + (original.IsYouzhou?"youzhou-":"") + state + ".json", json);
            var restored = JsonUtility.FromJson<World>(json);
            string error;
            if (restored == null) throw new Exception("Unity JSON serialization returned null.");
            if (!restored.IsValid(out error)) throw new Exception("Unity JSON serialization invalid state: " + error);
            if (Math.Abs(restored.resources.grain - original.resources.grain) > .000001 || restored.cards.Count != original.cards.Count)
                throw new Exception("Unity JSON serialization changed resources or cards.");
            if ((original.order == null) != (restored.order == null)) throw new Exception("Army idle state changed on load.");
            if (original.order != null && (original.order.target != restored.order.target || original.order.returning != restored.order.returning || Math.Abs(original.order.remaining - restored.order.remaining) > .000001))
                throw new Exception("Army march state changed on load.");
            if (original.OwnedCount != restored.OwnedCount || original.reports.Count != restored.reports.Count)
                throw new Exception("Territory or reports changed on load.");
        }
    }
}
