using System.Collections.Generic;
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

        public string MoveName => moveName;
        public string InputCommand => inputCommand;
        public string Description => description;
        public string VisualEffect => visualEffect;
        public float Damage => damage;
        public float Cooldown => cooldown;
        public string VoiceLine => voiceLine;
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

        private void OnValidate()
        {
            // Impede valores inválidos mesmo quando o asset é alterado pelo Inspector.
            maxHealth = Mathf.Max(1f, maxHealth);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            jumpForce = Mathf.Max(0f, jumpForce);
            punchDamage = Mathf.Max(0f, punchDamage);
            kickDamage = Mathf.Max(0f, kickDamage);
        }
    }
}
