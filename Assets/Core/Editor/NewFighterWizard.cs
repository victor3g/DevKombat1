#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using DevKombat.Fighters;
using DevKombat.Kombat;
using UnityEditor;
using UnityEngine;

namespace DevKombat.Editor
{
    /// <summary>
    /// Cria um lutador novo (amigo) a partir de um modelo do Mortal Kombat:
    /// pasta própria, ficha em branco e FighterData já com os especiais e o Fatality do modelo.
    /// Depois é só renomear os golpes, trocar comandos e colocar os sprites.
    /// </summary>
    public class NewFighterWizard : EditorWindow
    {
        private string fighterName = "Novo Amigo";
        private string nickname = "";
        private bool female;
        private Color costume = new Color(0.95f, 0.78f, 0.15f);
        private int template;
        private List<FighterData> templates;

        private void OnEnable()
        {
            templates = KombatTemplates.Create();
        }

        [MenuItem("Dev Kombat/Novo Lutador (amigo)...", priority = 20)]
        public static void Open()
        {
            NewFighterWizard window = GetWindow<NewFighterWizard>(true, "Novo Lutador");
            window.minSize = new Vector2(420f, 300f);
        }

        private void OnGUI()
        {
            string[] names = new string[templates.Count];
            for (int i = 0; i < templates.Count; i++)
            {
                names[i] = templates[i].FighterTitle;
            }

            EditorGUILayout.HelpBox(
                "Escolha de qual personagem do MK o amigo vai herdar as habilidades. " +
                "Depois edite o FighterData criado: nomes dos golpes, comandos, dano, falas, sons e sprites.",
                MessageType.Info);
            fighterName = EditorGUILayout.TextField("Nome do amigo", fighterName);
            nickname = EditorGUILayout.TextField("Apelido", nickname);
            female = EditorGUILayout.Toggle("Personagem feminina", female);
            costume = EditorGUILayout.ColorField("Cor da roupa (ninja)", costume);
            template = EditorGUILayout.Popup("Habilidades do MK", template, names);

            FighterData selected = templates[Mathf.Clamp(template, 0, templates.Count - 1)];
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Golpes que serão copiados:", EditorStyles.boldLabel);
            foreach (FighterSpecialMove move in selected.SpecialMoves)
            {
                EditorGUILayout.LabelField($"• {move.MoveName}", move.InputCommand);
            }

            if (selected.Fatality != null)
            {
                EditorGUILayout.LabelField($"• Fatality: {selected.Fatality.MoveName}", selected.Fatality.InputCommand);
            }

            EditorGUILayout.Space();
            GUI.enabled = !string.IsNullOrWhiteSpace(fighterName);
            if (GUILayout.Button("Criar lutador", GUILayout.Height(32f)))
            {
                Create(selected);
            }

            GUI.enabled = true;
        }

        private void Create(FighterData source)
        {
            string safe = MakeSafe(fighterName);
            string folder = $"Assets/Fighters/{safe}";
            if (AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog("Novo Lutador", $"Já existe a pasta {folder}.", "OK");
                return;
            }

            DevKombatKombatSetup.EnsureFolder("Assets", "Fighters");
            AssetDatabase.CreateFolder("Assets/Fighters", safe);
            AssetDatabase.CreateFolder(folder, "Sprites");
            AssetDatabase.CreateFolder(folder, "Audio");

            FighterData data = Instantiate(source);
            data.name = safe + "_Data";
            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("fighterName").stringValue = fighterName.Trim();
            serialized.FindProperty("fighterTitle").stringValue = string.IsNullOrWhiteSpace(nickname) ? source.FighterTitle : nickname.Trim();
            serialized.FindProperty("isFemale").boolValue = female;
            serialized.FindProperty("costumeColor").colorValue = costume;
            serialized.FindProperty("dialogueLines").ClearArray();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string assetPath = $"{folder}/{safe}_Data.asset";
            AssetDatabase.CreateAsset(data, assetPath);

            string fichaFolder = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Fichas", safe);
            string fichaBase = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Fichas", "FichaBase.md");
            if (File.Exists(fichaBase) && !Directory.Exists(fichaFolder))
            {
                Directory.CreateDirectory(fichaFolder);
                string ficha = File.ReadAllText(fichaBase).Replace("[NOME DO PERSONAGEM]", fighterName.Trim());
                File.WriteAllText(Path.Combine(fichaFolder, safe + ".md"), ficha);
            }

            AssetDatabase.SaveAssets();
            DevKombatKombatSetup.UpdateRoster();
            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
            Debug.Log($"Dev Kombat: {fighterName} criado em {assetPath} com as habilidades de \"{source.FighterTitle}\".");
            Close();
        }

        private static string MakeSafe(string text)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(c, '_');
            }

            return text.Trim().Replace(' ', '_');
        }
    }
}
#endif
