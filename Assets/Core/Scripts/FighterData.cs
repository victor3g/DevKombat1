using System.Collections.Generic;
using DevKombat.Kombat;
using UnityEngine;

namespace DevKombat.Fighters
{
    public enum FighterArchetype
    {
        Balanced,
        Rushdown,
        Zoner,
        Grappler
    }

    /// <summary>Descrição configurável de um golpe especial.</summary>
    [System.Serializable]
    public class FighterSpecialMove
    {
        [SerializeField] private string moveName;
        [SerializeField] private string inputCommand;
        [TextArea(2, 5)] [SerializeField] private string description;
        [TextArea(2, 4)] [SerializeField] private string visualEffect;
        [Min(0f)] [SerializeField] private float damage;
        [Min(0f)] [SerializeField] private float cooldown;
        [TextArea(1, 3)] [SerializeField] private string voiceLine;

        [Header("Kombat")]
        [Tooltip("Habilidade do Mortal Kombat usada por este golpe.")]
        [SerializeField] private MKAbility ability;
        [Tooltip("Quadros próprios do efeito (ex.: café voando). Vazio usa o efeito original do MK.")]
        [SerializeField] private Sprite[] effectFrames;
        [Tooltip("Cor aplicada ao efeito. Branco mantém as cores originais.")]
        [SerializeField] private Color effectTint = Color.white;
        [Tooltip("Som próprio do golpe. Vazio usa o som padrão da habilidade.")]
        [SerializeField] private AudioClip sound;

        public FighterSpecialMove()
        {
        }

        public FighterSpecialMove(string name, string command, MKAbility mkAbility, float moveDamage, string line = null)
        {
            moveName = name;
            inputCommand = command;
            ability = mkAbility;
            damage = moveDamage;
            voiceLine = line;
        }

        public string MoveName => moveName;
        public string InputCommand => inputCommand;
        public string Description => description;
        public string VisualEffect => visualEffect;
        public float Damage => damage;
        public float Cooldown => cooldown;
        public string VoiceLine => voiceLine;
        public MKAbility Ability => ability;
        public Sprite[] EffectFrames => effectFrames;
        public Color EffectTint => effectTint;
        public AudioClip Sound => sound;
        public bool IsConfigured => ability != MKAbility.None && !string.IsNullOrWhiteSpace(inputCommand);
    }

    /// <summary>
    /// Reúne toda a configuração de um lutador em um único asset reutilizável.
    /// Crie uma instância pelo menu Assets/Create/Dev Kombat/Fighter Data.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NovoFighterData",
        menuName = "Dev Kombat/Fighter Data",
        order = 0)]
    public class FighterData : ScriptableObject
    {
        [Header("Identidade")]
        [SerializeField] private string fighterName = "Novo Lutador";
        [SerializeField] private string fighterTitle;
        [TextArea(2, 4)] [SerializeField] private string selectionDescription;
        [SerializeField] private Sprite selectionPortrait;

        [Header("Perfil de gameplay")]
        [SerializeField] private FighterArchetype archetype = FighterArchetype.Balanced;
        [TextArea(2, 4)] [SerializeField] private string fightingStyle;
        [SerializeField] private string preferredRange;
        [SerializeField] private string mainStrength;
        [SerializeField] private string mainWeakness;

        [Header("Atributos de combate")]
        [Min(1f)]
        [SerializeField] private float maxHealth = 100f;
        [Min(0f)]
        [SerializeField] private float moveSpeed = 5f;
        [Min(0f)]
        [SerializeField] private float jumpForce = 12f;
        [Min(0f)]
        [SerializeField] private float punchDamage = 10f;
        [Min(0f)]
        [SerializeField] private float kickDamage = 15f;

        [Header("Descrição dos golpes básicos")]
        [TextArea(2, 4)] [SerializeField] private string punchDescription;
        [TextArea(2, 4)] [SerializeField] private string kickDescription;
        [TextArea(2, 4)] [SerializeField] private string crouchingAttackDescription;
        [TextArea(2, 4)] [SerializeField] private string aerialAttackDescription;
        [TextArea(2, 4)] [SerializeField] private string throwDescription;

        [Header("Golpes especiais")]
        [SerializeField] private List<FighterSpecialMove> specialMoves = new List<FighterSpecialMove>();
        [SerializeField] private FighterSpecialMove ultimateMove;

        [Header("Áudio")]
        [Tooltip("Falas reproduzidas na entrada do lutador.")]
        [SerializeField] private List<AudioClip> introClips = new List<AudioClip>();
        [Tooltip("Sons reproduzidos quando o lutador recebe dano.")]
        [SerializeField] private List<AudioClip> hitClips = new List<AudioClip>();
        [Tooltip("Sons reproduzidos durante os ataques.")]
        [SerializeField] private List<AudioClip> attackClips = new List<AudioClip>();
        [Tooltip("Frases reproduzidas ao vencer uma luta.")]
        [SerializeField] private List<AudioClip> victoryClips = new List<AudioClip>();

        [Header("Animação")]
        [SerializeField] private RuntimeAnimatorController animatorController;

        [Header("Diálogos")]
        [TextArea(2, 4)]
        [SerializeField] private List<string> dialogueLines = new List<string>();

        [Header("Kombat (estilo Mortal Kombat)")]
        [Tooltip("Usa \"FINISH HER\" e vozes femininas quando não houver áudio próprio.")]
        [SerializeField] private bool isFemale;
        [Tooltip("Cor da roupa do ninja usado enquanto o lutador não tem sprites próprios.")]
        [SerializeField] private Color costumeColor = new Color(0.95f, 0.78f, 0.15f);
        [Tooltip("Escala visual dos sprites próprios (1 = altura do ninja do MK2).")]
        [Min(0.1f)] [SerializeField] private float visualScale = 1f;
        [Tooltip("Sprites próprios por animação. Animações vazias usam o ninja do MK2 tingido.")]
        [SerializeField] private FighterSpriteSet sprites = new FighterSpriteSet();
        [Tooltip("Golpe final executado durante o FINISH HIM/HER.")]
        [SerializeField] private FighterSpecialMove fatality;
        [Tooltip("Locutor dizendo o nome do lutador (opcional).")]
        [SerializeField] private AudioClip announcerName;

        public string FighterName => fighterName;
        public string FighterTitle => fighterTitle;
        public string SelectionDescription => selectionDescription;
        public Sprite SelectionPortrait => selectionPortrait;
        public FighterArchetype Archetype => archetype;
        public string FightingStyle => fightingStyle;
        public string PreferredRange => preferredRange;
        public string MainStrength => mainStrength;
        public string MainWeakness => mainWeakness;

        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float JumpForce => jumpForce;
        public float PunchDamage => punchDamage;
        public float KickDamage => kickDamage;
        public string PunchDescription => punchDescription;
        public string KickDescription => kickDescription;
        public string CrouchingAttackDescription => crouchingAttackDescription;
        public string AerialAttackDescription => aerialAttackDescription;
        public string ThrowDescription => throwDescription;
        public IReadOnlyList<FighterSpecialMove> SpecialMoves => specialMoves;
        public FighterSpecialMove UltimateMove => ultimateMove;

        public IReadOnlyList<AudioClip> IntroClips => introClips;
        public IReadOnlyList<AudioClip> HitClips => hitClips;
        public IReadOnlyList<AudioClip> AttackClips => attackClips;
        public IReadOnlyList<AudioClip> VictoryClips => victoryClips;

        public RuntimeAnimatorController AnimatorController => animatorController;
        public IReadOnlyList<string> DialogueLines => dialogueLines;

        public bool IsFemale => isFemale;
        public Color CostumeColor => costumeColor;
        public float VisualScale => visualScale;
        public FighterSpriteSet Sprites => sprites;
        public FighterSpecialMove Fatality => fatality;
        public AudioClip AnnouncerName => announcerName;

        /// <summary>
        /// Monta um lutador em tempo de execução. Usado pelos modelos de demonstração
        /// que mostram cada conjunto de habilidades do Mortal Kombat.
        /// </summary>
        public static FighterData CreateRuntime(
            string name,
            string title,
            string description,
            Color costume,
            bool female,
            FighterArchetype type,
            IEnumerable<FighterSpecialMove> specials,
            FighterSpecialMove finisher,
            IEnumerable<string> lines)
        {
            FighterData data = CreateInstance<FighterData>();
            data.name = name;
            data.fighterName = name;
            data.fighterTitle = title;
            data.selectionDescription = description;
            data.costumeColor = costume;
            data.isFemale = female;
            data.archetype = type;
            data.specialMoves = new List<FighterSpecialMove>(specials);
            data.fatality = finisher;
            data.dialogueLines = new List<string>(lines);
            return data;
        }

        private void OnValidate()
        {
            // Impede valores inválidos mesmo quando o asset é alterado pelo Inspector.
            maxHealth = Mathf.Max(1f, maxHealth);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            jumpForce = Mathf.Max(0f, jumpForce);
            punchDamage = Mathf.Max(0f, punchDamage);
            kickDamage = Mathf.Max(0f, kickDamage);
            visualScale = Mathf.Max(0.1f, visualScale);
        }
    }
}
