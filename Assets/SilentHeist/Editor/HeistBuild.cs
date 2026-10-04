#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SilentHeist.Editor
{
    public static class HeistBuild
    {
        public const string ScenePath="Assets/SilentHeist/Scenes/Boot.unity";
        [InitializeOnLoadMethod]
        private static void OnEditorLoad(){EditorApplication.delayCall+=EnsureProject;}
        private static void EnsureProject()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            // Configuration is a real editable asset, not constants buried in the controller.
            if(!File.Exists("Assets/SilentHeist/Resources/GameConfig.asset"))
            {
                Directory.CreateDirectory("Assets/SilentHeist/Resources");
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameConfig>(),"Assets/SilentHeist/Resources/GameConfig.asset");
                AssetDatabase.SaveAssets();
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            // The runtime vision mesh needs this shader in player builds, too.
            var graphicsAssets=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if(graphicsAssets.Length>0)
            {
                var settings=new SerializedObject(graphicsAssets[0]);
                var shaders=settings.FindProperty("m_AlwaysIncludedShaders");
                var spriteShader=Shader.Find("Sprites/Default");
                if(shaders!=null&&spriteShader!=null)
                {
                    bool found=false;
                    for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==spriteShader)found=true;
                    if(!found){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=spriteShader;settings.ApplyModifiedProperties();}
                }
            }
        }
        [MenuItem("Silent Heist/Open Game Scene")]
        public static void OpenGame(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);}
        [MenuItem("Silent Heist/Build Windows")]
        public static void BuildWindows()
        {
            string path=EditorUtility.SaveFolderPanel("Windows build folder","Builds","");if(string.IsNullOrEmpty(path))return;
            Build(path+"/SilentHeist.exe",BuildTarget.StandaloneWindows64);
        }
        [MenuItem("Silent Heist/Build Android APK")]
        public static void BuildAndroid()
        {
            string path=EditorUtility.SaveFilePanel("Android APK","Builds","SilentHeist","apk");if(string.IsNullOrEmpty(path))return;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.abedalqader.silentheist");
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle=false;Build(path,BuildTarget.Android);
        }
        private static void Build(string path,BuildTarget target)
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target),target))
            {EditorUtility.DisplayDialog("Build support needed","Install the selected platform module in Unity Hub > Installs > 6000.3.20f1 > Add modules. Android also needs SDK/NDK and OpenJDK.","OK");return;}
            EnsureProject();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=path,target=target,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed. Read the first red error in the Console.");
            EditorUtility.RevealInFinder(path);Debug.Log("Silent Heist build succeeded: "+path);
        }
        [MenuItem("Silent Heist/Run Rule Checks")]
        public static void RunChecks()
        {
            for(int i=0;i<3;i++)
            {
                var w=new GridWorld(LevelDefinition.Create(i));
                if(w.Solid(w.Definition.Start))throw new Exception("Blocked player spawn");
                if(w.Definition.HasSwitch)
                {
                    if(w.NextStep(w.Definition.Start,w.Definition.Switch)==w.Definition.Start)throw new Exception("Switch unreachable");
                    w.OpenDoors();
                }
                foreach(Cell c in w.Definition.Loot)
                {
                    if(w.NextStep(w.Definition.Start,c)==w.Definition.Start)throw new Exception("Loot unreachable "+i+" / "+c);
                    if(w.NextStep(c,w.Definition.Exit)==c)throw new Exception("Exit unreachable "+i+" / "+c);
                }
            }
            Debug.Log("Silent Heist: all three level route checks passed.");
        }
    }
}
#endif
