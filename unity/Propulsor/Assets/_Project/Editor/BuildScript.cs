using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Propulsor.EditorTools
{
    /// <summary>
    /// Builds reproduzíveis (menu "Propulsor" no editor ou linha de comando):
    ///   Unity -batchmode -quit -projectPath unity/Propulsor -executeMethod Propulsor.EditorTools.BuildScript.BuildAndroid
    ///   Unity -batchmode -quit -projectPath unity/Propulsor -executeMethod Propulsor.EditorTools.BuildScript.BuildWebGL
    /// Cria a cena principal (vazia — o jogo é montado pelo PrototypeBootstrap) se ela não existir.
    /// </summary>
    public static class BuildScript
    {
        public const string ScenePath = "Assets/_Project/Scenes/Principal.unity";
        private const string OutputRoot = "Builds";

        [MenuItem("Propulsor/Preparar projeto (cena e ajustes)")]
        public static void Prepare()
        {
            EnsureScene();
            PlayerSettings.companyName = "Projeto Propulsor";
            PlayerSettings.productName = "PROPULSOR";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.runInBackground = false;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("PROPULSOR: projeto preparado (cena principal, paisagem, nome do produto).");
        }

        [MenuItem("Propulsor/Build Android (APK)")]
        public static void BuildAndroid()
        {
            Prepare();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.projetopropulsor.jogo");
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, Path.Combine(OutputRoot, "Android", "Propulsor.apk"));
        }

        [MenuItem("Propulsor/Build WebGL")]
        public static void BuildWebGL()
        {
            Prepare();
            Build(BuildTarget.WebGL, Path.Combine(OutputRoot, "WebGL"));
        }

        private static void Build(BuildTarget target, string output)
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = target,
                locationPathName = output,
                options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"PROPULSOR: build {target} OK — {summary.totalSize / (1024 * 1024)} MB em {output}");
            else
            {
                Debug.LogError($"PROPULSOR: build {target} falhou ({summary.totalErrors} erros).");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void EnsureScene()
        {
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Cena vazia de propósito: câmera, mundo, áudio e telas são criados pelo PrototypeBootstrap.
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("PROPULSOR: cena principal criada em " + ScenePath);
        }
    }
}
