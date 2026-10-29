using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace EasternDynasty.Editor
{
    // Invoked by Tools/Build-Android.ps1 with -buildTarget Android so scripting
    // defines and imported assets already correspond to the target platform.
    public static class AndroidBuildTools
    {
        public const string ApplicationId = "com.easterndynasty.s1";
        private const string ScenePath = "Assets/Scenes/EasternDynasty.unity";

        [Serializable] private sealed class AndroidBuildReceipt
        {
            public string applicationId, versionName, output, unityVersion, completedUtc;
            public string backend, architectures, graphicsApi, signing;
            public int versionCode, minimumApi;
            public long totalBytes;
            public bool development, settingsRestored;
        }

        private static string Argument(string name, string fallback = null)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
            return fallback;
        }

        public static void BuildApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Start Unity with -buildTarget Android before BuildApk.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Install this Editor's Android Build Support, SDK/NDK and OpenJDK modules.");
            string requested = Argument("-edAndroidOutput");
            if (string.IsNullOrWhiteSpace(requested) || !requested.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("-edAndroidOutput must specify a new .apk path.");
            string output = Path.GetFullPath(requested);
            if (File.Exists(output)) throw new IOException("Refusing to reuse an existing APK candidate: " + output);
            int versionCode;
            if (!int.TryParse(Argument("-edAndroidVersionCode"), out versionCode) || versionCode < 1)
                throw new ArgumentException("A positive -edAndroidVersionCode is required.");
            string versionName = Argument("-edAndroidVersionName", "0.2.0");
            if (!System.Text.RegularExpressions.Regex.IsMatch(versionName, @"^[0-9A-Za-z][0-9A-Za-z._-]{0,63}$"))
                throw new ArgumentException("Invalid Android version name.");

            var previous = new SettingsSnapshot();
            var receipt = new AndroidBuildReceipt();
            try
            {
                ProjectTools.Prepare();
                Configure(versionName, versionCode);
                AssetCatalogTools.ValidateForBuild();
                foreach (string path in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/EasternDynasty/Resources" }).Select(AssetDatabase.GUIDToAssetPath))
                {
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                    if (ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Shader has compile errors: " + path);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                AssetDatabase.SaveAssets();
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath }, locationPathName = output,
                    target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.CompressWithLz4HC
                });
                if (report.summary.result != BuildResult.Succeeded || !File.Exists(output))
                    throw new InvalidOperationException("Android build failed: " + report.summary.result);
                receipt.applicationId = ApplicationId;
                receipt.versionName = versionName; receipt.versionCode = versionCode;
                receipt.minimumApi = 25; receipt.backend = "IL2CPP";
                receipt.architectures = "ARM64, ARMv7"; receipt.graphicsApi = "OpenGLES3";
                receipt.signing = "Unity local debug key; APK signature must be verified by the wrapper";
                receipt.output = output; receipt.unityVersion = Application.unityVersion;
                receipt.totalBytes = new FileInfo(output).Length;
                receipt.completedUtc = DateTime.UtcNow.ToString("o");
                receipt.development = false;
            }
            finally
            {
                previous.Restore();
                AssetDatabase.SaveAssets();
            }
            receipt.settingsRestored = true;
            File.WriteAllText(output + ".build.json", JsonUtility.ToJson(receipt, true));
            Debug.Log("EASTERN_DYNASTY_ANDROID_BUILD_OK: " + output);
        }

        private static void Configure(string versionName, int versionCode)
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.bundleVersion = versionName;
            PlayerSettings.Android.bundleVersionCode = versionCode;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.Android.renderOutsideSafeArea = false;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.Android.splitApplicationBinary = false;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            PlayerSettings.Android.minifyDebug = false;
            PlayerSettings.Android.minifyRelease = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
        }

        public static void VerifyWindowsRestored()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Windows target was not restored.");
            Debug.Log("EASTERN_DYNASTY_ANDROID_WINDOWS_RESTORED");
        }

        private sealed class SettingsSnapshot
        {
            private readonly string applicationId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            private readonly string version = PlayerSettings.bundleVersion;
            private readonly int versionCode = PlayerSettings.Android.bundleVersionCode;
            private readonly ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            private readonly ManagedStrippingLevel stripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Android);
            private readonly bool stripEngine = PlayerSettings.stripEngineCode;
            private readonly AndroidArchitecture architectures = PlayerSettings.Android.targetArchitectures;
            private readonly AndroidSdkVersions minApi = PlayerSettings.Android.minSdkVersion, targetApi = PlayerSettings.Android.targetSdkVersion;
            private readonly bool defaultGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            private readonly GraphicsDeviceType[] graphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            private readonly UIOrientation orientation = PlayerSettings.defaultInterfaceOrientation;
            private readonly bool portrait = PlayerSettings.allowedAutorotateToPortrait, upsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            private readonly bool landscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft, landscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;
            private readonly FullScreenMode fullscreen = PlayerSettings.fullScreenMode;
            private readonly bool outsideSafeArea = PlayerSettings.Android.renderOutsideSafeArea, customKey = PlayerSettings.Android.useCustomKeystore;
            private readonly AndroidApplicationEntry entry = PlayerSettings.Android.applicationEntry;
            private readonly bool expansion = PlayerSettings.Android.splitApplicationBinary, split = PlayerSettings.Android.buildApkPerCpuArchitecture;
            private readonly bool minifyDebug = PlayerSettings.Android.minifyDebug, minifyRelease = PlayerSettings.Android.minifyRelease;
            private readonly bool appBundle = EditorUserBuildSettings.buildAppBundle, export = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            private readonly MobileTextureSubtarget texture = EditorUserBuildSettings.androidBuildSubtarget;
            public void Restore()
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, applicationId);
                PlayerSettings.bundleVersion = version; PlayerSettings.Android.bundleVersionCode = versionCode;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend);
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, stripping);
                PlayerSettings.stripEngineCode = stripEngine; PlayerSettings.Android.targetArchitectures = architectures;
                PlayerSettings.Android.minSdkVersion = minApi; PlayerSettings.Android.targetSdkVersion = targetApi;
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphics);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, defaultGraphics);
                PlayerSettings.defaultInterfaceOrientation = orientation;
                PlayerSettings.allowedAutorotateToPortrait = portrait; PlayerSettings.allowedAutorotateToPortraitUpsideDown = upsideDown;
                PlayerSettings.allowedAutorotateToLandscapeLeft = landscapeLeft; PlayerSettings.allowedAutorotateToLandscapeRight = landscapeRight;
                PlayerSettings.fullScreenMode = fullscreen; PlayerSettings.Android.renderOutsideSafeArea = outsideSafeArea;
                PlayerSettings.Android.useCustomKeystore = customKey; PlayerSettings.Android.applicationEntry = entry;
                PlayerSettings.Android.splitApplicationBinary = expansion; PlayerSettings.Android.buildApkPerCpuArchitecture = split;
                PlayerSettings.Android.minifyDebug = minifyDebug; PlayerSettings.Android.minifyRelease = minifyRelease;
                EditorUserBuildSettings.buildAppBundle = appBundle; EditorUserBuildSettings.exportAsGoogleAndroidProject = export;
                EditorUserBuildSettings.androidBuildSubtarget = texture;
            }
        }
    }
}
