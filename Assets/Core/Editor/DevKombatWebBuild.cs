#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DevKombat.Editor
{
    /// <summary>Gera um build WebGL de desenvolvimento pronto para hospedagem.</summary>
    public static class DevKombatWebBuild
    {
        private const string ScenePath = "Assets/Core/Scenes/PrototypeArena.unity";
        private const string OutputPath = "Builds/WebGL";

        [MenuItem("Dev Kombat/Gerar Build WebGL de Teste")]
        public static void BuildWebGL()
        {
            if (!File.Exists(ScenePath))
            {
                DevKombatProjectSetup.PrepareFirstTest();
            }

            Directory.CreateDirectory(OutputPath);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build WebGL concluído em {OutputPath} ({report.summary.totalSize} bytes).");
                EditorUtility.RevealInFinder(OutputPath);
            }
            else
            {
                Debug.LogError($"Build WebGL falhou: {report.summary.result}. Verifique o Console.");
            }
        }
    }
}
#endif
