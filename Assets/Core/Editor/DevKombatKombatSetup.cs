#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using DevKombat.Fighters;
using DevKombat.Kombat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevKombat.Editor
{
    /// <summary>
    /// Monta a cena do jogo estilo Mortal Kombat e a lista de lutadores.
    /// Roda sozinho na primeira abertura e pode ser repetido pelo menu.
    /// </summary>
    public static class DevKombatKombatSetup
    {
        public const string ScenePath = "Assets/Core/Scenes/KombatArena.unity";
        public const string RosterPath = "Assets/Core/Data/KombatRoster.asset";
        private const string PrototypeScenePath = "Assets/Core/Scenes/PrototypeArena.unity";

        [InitializeOnLoadMethod]
        private static void ScheduleAutomaticSetup()
        {
            EditorApplication.delayCall += RunAutomaticSetup;
        }

        private static void RunAutomaticSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunAutomaticSetup;
                return;
            }

            if (!File.Exists(ScenePath))
            {
                PrepareKombat();
            }
        }

        [MenuItem("Dev Kombat/Preparar Kombat (estilo MK)", priority = 0)]
        public static void PrepareKombat()
        {
            ImportFighterSprites();
            KombatRoster roster = UpdateRoster();
            CreateScene(roster);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Directory.Exists("Assets/Resources/Kombat"))
            {
                Debug.LogWarning("Dev Kombat: rode \"python Tools/extrair_assets_mk.py\" na pasta do projeto para " +
                                 "baixar cenários, efeitos, interface e sons do MK. Sem eles o jogo usa formas simples.");
            }

            Debug.Log("Dev Kombat: abra Assets/Core/Scenes/KombatArena.unity e pressione Play.");
        }

        [MenuItem("Dev Kombat/Atualizar lista de lutadores", priority = 1)]
        public static KombatRoster UpdateRoster()
        {
            EnsureFolder("Assets/Core", "Data");
            KombatRoster roster = AssetDatabase.LoadAssetAtPath<KombatRoster>(RosterPath);
            if (roster == null)
            {
                roster = ScriptableObject.CreateInstance<KombatRoster>();
                AssetDatabase.CreateAsset(roster, RosterPath);
            }

            List<FighterData> found = new List<FighterData>();
            foreach (string guid in AssetDatabase.FindAssets("t:FighterData"))
            {
                FighterData data = AssetDatabase.LoadAssetAtPath<FighterData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data != null)
                {
                    found.Add(data);
                }
            }

            found.Sort((a, b) => string.CompareOrdinal(a.FighterName, b.FighterName));
            roster.fighters = found;
            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssets();
            Debug.Log($"Dev Kombat: {found.Count} lutador(es) na lista de seleção.");
            return roster;
        }

        // Palavra no nome do arquivo -> campo do FighterSpriteSet.
        private static readonly string[] SpriteKeys =
        {
            "idle", "walk", "crouch", "jump", "block", "punch", "kick", "uppercut", "sweep", "special", "grab",
            "hit", "knockdown", "dizzy", "victory", "defeat", "taunt"
        };

        /// <summary>
        /// Preenche os sprites de cada FighterData pelos nomes dos arquivos na pasta Sprites do lutador.
        /// Ex.: spr_mariana_idle_01.png, spr_mariana_idle_02.png, spr_mariana_kick_01.png.
        /// Só preenche animações que ainda estão vazias.
        /// </summary>
        [MenuItem("Dev Kombat/Importar sprites dos lutadores", priority = 2)]
        public static void ImportFighterSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:FighterData"))
            {
                string dataPath = AssetDatabase.GUIDToAssetPath(guid);
                FighterData data = AssetDatabase.LoadAssetAtPath<FighterData>(dataPath);
                string folder = Path.GetDirectoryName(dataPath)?.Replace('\\', '/') + "/Sprites";
                if (data == null || !AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(data);
                bool changed = false;
                foreach (string key in SpriteKeys)
                {
                    SerializedProperty list = serialized.FindProperty("sprites." + key);
                    if (list == null || list.arraySize > 0)
                    {
                        continue;
                    }

                    List<Sprite> sprites = FindSprites(folder, key);
                    if (sprites.Count == 0 && key == "knockdown")
                    {
                        sprites = FindSprites(folder, "defeat");
                    }

                    list.arraySize = sprites.Count;
                    for (int i = 0; i < sprites.Count; i++)
                    {
                        list.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                    }

                    changed |= sprites.Count > 0;
                }

                if (changed)
                {
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(data);
                    Debug.Log($"Dev Kombat: sprites de {data.FighterName} importados de {folder}.");
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static List<Sprite> FindSprites(string folder, string key)
        {
            List<string> paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path)?.Replace('\\', '/') != folder)
                {
                    continue;
                }

                string[] parts = Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Split('_', '-', ' ');
                if (System.Array.IndexOf(parts, key) >= 0)
                {
                    paths.Add(path);
                }
            }

            paths.Sort(string.CompareOrdinal);
            List<Sprite> sprites = new List<Sprite>();
            foreach (string path in paths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 512f;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites;
        }

        private static void CreateScene(KombatRoster roster)
        {
            EnsureFolder("Assets/Core", "Scenes");
            Scene previous = SceneManager.GetActiveScene();
            bool restore = !string.IsNullOrEmpty(previous.path) && previous.path != ScenePath;
            string previousPath = previous.path;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Dev Kombat: preparação cancelada para não perder a cena aberta.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject game = new GameObject("DevKombat");
            KombatGame kombat = game.AddComponent<KombatGame>();
            SerializedObject serialized = new SerializedObject(kombat);
            serialized.FindProperty("roster").objectReferenceValue = roster;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = KombatStage.ViewHeight * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 3f, -10f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            if (restore && File.Exists(previousPath))
            {
                EditorSceneManager.OpenScene(previousPath);
            }
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            if (File.Exists(PrototypeScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(PrototypeScenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
