#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using DevKombat.Fighters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevKombat.Editor
{
    /// <summary>
    /// Configura automaticamente o protótipo na primeira abertura do projeto.
    /// Também pode ser executado novamente pelo menu Dev Kombat/Preparar Primeiro Teste.
    /// </summary>
    public static class DevKombatProjectSetup
    {
        private const string FighterPrefabPath = "Assets/Fighters/Mariana_DevSenior/Mariana.prefab";
        private const string ScenePath = "Assets/Core/Scenes/PrototypeArena.unity";
        private const string SpriteFolder = "Assets/Fighters/Mariana_DevSenior/Sprites";

        private static readonly Dictionary<string, string> SpriteFiles = new Dictionary<string, string>
        {
            { "idleFrames", "spr_mariana_idle_01.png" },
            { "walkFrames", "spr_mariana_walk_01.png" },
            { "jumpFrames", "spr_mariana_jump_01.png" },
            { "punchFrames", "spr_mariana_punch_01.png" },
            { "kickFrames", "spr_mariana_kick_01.png" },
            { "hitFrames", "spr_mariana_hit_01.png" },
            { "defeatFrames", "spr_mariana_defeat_01.png" },
            { "victoryFrames", "spr_mariana_victory_01.png" }
        };

        [InitializeOnLoadMethod]
        private static void ScheduleAutomaticSetup()
        {
            EditorApplication.delayCall += RunAutomaticSetup;
        }

        private static void RunAutomaticSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunAutomaticSetup;
                return;
            }

            if (!File.Exists(ScenePath))
            {
                PrepareFirstTest();
            }
        }

        [MenuItem("Dev Kombat/Preparar Primeiro Teste")]
        public static void PrepareFirstTest()
        {
            ConfigureSpriteImporters();
            AssignSpritesToMariana();
            CreatePrototypeScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dev Kombat: primeiro teste preparado. Abra PrototypeArena e pressione Play.");
        }

        private static void ConfigureSpriteImporters()
        {
            foreach (string fileName in SpriteFiles.Values)
            {
                string path = $"{SpriteFolder}/{fileName}";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"Sprite não encontrado: {path}");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 512f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void AssignSpritesToMariana()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(FighterPrefabPath);
            try
            {
                PrototypeFighter2D prototype = root.GetComponent<PrototypeFighter2D>();
                if (prototype == null)
                {
                    Debug.LogError("O prefab da Mariana não contém PrototypeFighter2D.");
                    return;
                }

                SerializedObject serialized = new SerializedObject(prototype);
                foreach (KeyValuePair<string, string> entry in SpriteFiles)
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{entry.Value}");
                    SerializedProperty array = serialized.FindProperty(entry.Key);
                    array.arraySize = sprite != null ? 1 : 0;
                    if (sprite != null)
                    {
                        array.GetArrayElementAtIndex(0).objectReferenceValue = sprite;
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, FighterPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreatePrototypeScene()
        {
            string sceneFolder = Path.GetDirectoryName(ScenePath)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(sceneFolder))
            {
                AssetDatabase.CreateFolder("Assets/Core", "Scenes");
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject managerObject = new GameObject("PrototypeArena");
            PrototypeArenaBootstrap bootstrap = managerObject.AddComponent<PrototypeArenaBootstrap>();

            GameObject fighterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FighterPrefabPath);
            SerializedObject serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("fighterPrefab").objectReferenceValue = fighterPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }
    }
}
#endif
