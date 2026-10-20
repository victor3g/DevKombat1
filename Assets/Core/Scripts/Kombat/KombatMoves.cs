using System.Collections.Generic;
using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>
    /// Dados de quadro de um golpe (60 quadros por segundo), no estilo MK2.
    /// Hitbox em unidades do mundo, relativa aos pés do lutador virado para a direita.
    /// </summary>
    public class KombatMove
    {
        public string Id;
        public string Animation;
        public int Startup;
        public int Active;
        public int Recovery;
        public float Damage;
        public Rect Hitbox;
        public HitLevel Level = HitLevel.Mid;
        public HitReaction Reaction = HitReaction.Stagger;
        public float Pushback = 1.2f;
        public int Hitstun = 16;
        public bool IsKick;
        public bool Airborne;
        public string Sound = "swing";

        public int Total => Startup + Active + Recovery;

        public KombatMove Clone() => (KombatMove)MemberwiseClone();
    }

    /// <summary>Golpes normais compartilhados por todos os lutadores.</summary>
    public static class KombatMoves
    {
        public static readonly KombatMove LowPunch = new KombatMove
        {
            Id = "low_punch", Animation = "low_punch", Startup = 4, Active = 3, Recovery = 7, Damage = 3f,
            Hitbox = new Rect(0.25f, 2.55f, 1.05f, 0.55f), Level = HitLevel.High, Hitstun = 13, Pushback = 0.8f
        };

        public static readonly KombatMove HighPunch = new KombatMove
        {
            Id = "high_punch", Animation = "high_punch", Startup = 5, Active = 3, Recovery = 9, Damage = 4.5f,
            Hitbox = new Rect(0.25f, 2.7f, 1.15f, 0.55f), Level = HitLevel.High, Hitstun = 15
        };

        public static readonly KombatMove LowKick = new KombatMove
        {
            Id = "low_kick", Animation = "low_kick", Startup = 6, Active = 3, Recovery = 11, Damage = 5f,
            Hitbox = new Rect(0.25f, 1.1f, 1.25f, 0.7f), Level = HitLevel.Mid, Hitstun = 16, IsKick = true
        };

        public static readonly KombatMove HighKick = new KombatMove
        {
            Id = "high_kick", Animation = "high_kick", Startup = 7, Active = 4, Recovery = 13, Damage = 7f,
            Hitbox = new Rect(0.3f, 2.3f, 1.35f, 0.8f), Level = HitLevel.High, Hitstun = 18, IsKick = true,
            Pushback = 1.6f
        };

        public static readonly KombatMove Roundhouse = new KombatMove
        {
            Id = "roundhouse", Animation = "roundhouse", Startup = 9, Active = 4, Recovery = 18, Damage = 10f,
            Hitbox = new Rect(0.3f, 2.2f, 1.45f, 0.9f), Level = HitLevel.High, Reaction = HitReaction.Knockdown,
            IsKick = true, Sound = "swing_heavy"
        };

        public static readonly KombatMove Sweep = new KombatMove
        {
            Id = "sweep", Animation = "sweep", Startup = 8, Active = 4, Recovery = 20, Damage = 8f,
            Hitbox = new Rect(0.35f, 0f, 1.6f, 0.55f), Level = HitLevel.Low, Reaction = HitReaction.Sweep,
            IsKick = true, Sound = "swing_heavy"
        };

        public static readonly KombatMove Uppercut = new KombatMove
        {
            Id = "uppercut", Animation = "uppercut", Startup = 6, Active = 4, Recovery = 22, Damage = 14f,
            Hitbox = new Rect(0.1f, 1.6f, 1.1f, 2.3f), Level = HitLevel.Mid, Reaction = HitReaction.Launch,
            Sound = "swing_heavy"
        };

        public static readonly KombatMove CrouchPunch = new KombatMove
        {
            Id = "crouch_punch", Animation = "crouch_punch", Startup = 4, Active = 3, Recovery = 8, Damage = 3f,
            Hitbox = new Rect(0.25f, 1.3f, 1.0f, 0.55f), Level = HitLevel.Mid, Hitstun = 13
        };

        public static readonly KombatMove CrouchKick = new KombatMove
        {
            Id = "crouch_kick", Animation = "crouch_kick", Startup = 5, Active = 3, Recovery = 12, Damage = 4f,
            Hitbox = new Rect(0.3f, 0f, 1.3f, 0.5f), Level = HitLevel.Low, Hitstun = 14, IsKick = true
        };

        public static readonly KombatMove JumpPunch = new KombatMove
        {
            Id = "jump_punch", Animation = "jump_punch", Startup = 3, Active = 10, Recovery = 4, Damage = 6f,
            Hitbox = new Rect(0.2f, 0.9f, 1.1f, 1.0f), Level = HitLevel.Overhead, Airborne = true, Hitstun = 18
        };

        public static readonly KombatMove JumpKick = new KombatMove
        {
            Id = "jump_kick", Animation = "jump_kick", Startup = 3, Active = 12, Recovery = 4, Damage = 7f,
            Hitbox = new Rect(0.2f, 0.5f, 1.15f, 1.0f), Level = HitLevel.Overhead, Airborne = true, Hitstun = 18,
            IsKick = true
        };

        public static readonly KombatMove FlyingKick = new KombatMove
        {
            Id = "flying_kick", Animation = "flying_kick", Startup = 3, Active = 14, Recovery = 4, Damage = 9f,
            Hitbox = new Rect(0.3f, 0.4f, 1.3f, 1.0f), Level = HitLevel.Overhead, Reaction = HitReaction.Knockdown,
            Airborne = true, IsKick = true
        };

        public static readonly KombatMove Throw = new KombatMove
        {
            Id = "throw", Animation = "throw", Startup = 3, Active = 2, Recovery = 26, Damage = 12f,
            Hitbox = new Rect(0.2f, 1.0f, 0.9f, 1.6f), Level = HitLevel.Unblockable, Reaction = HitReaction.Knockdown
        };

        /// <summary>Lista para a tela de golpes (pausa).</summary>
        public static readonly List<(string command, string name)> MoveList = new List<(string, string)>
        {
            ("Soco Fraco / Soco Forte", "Socos"),
            ("Chute Fraco / Chute Forte", "Chutes"),
            ("Baixo + Soco Forte", "Gancho (joga para o alto)"),
            ("Trás + Chute Fraco", "Rasteira"),
            ("Trás + Chute Forte", "Chute giratório"),
            ("Baixo + Soco Fraco / Chute", "Golpes agachado"),
            ("No ar + Soco / Chute", "Golpes aéreos"),
            ("Perto + Frente + Soco Fraco", "Arremesso"),
            ("Segure Defesa", "Defender (agachado defende golpes baixos)")
        };
    }

    public enum AbilityKind
    {
        Projectile,
        Teleport,
        Dash,
        GroundPound,
        Grab,
        Lift,
        NutPunch,
        Ultimate,
        Fatality
    }

    /// <summary>Parâmetros padrão de uma habilidade do MK. O lutador pode mudar dano, efeito e som.</summary>
    public class AbilityPreset
    {
        public MKAbility Ability;
        public AbilityKind Kind;
        public string BodyAnimation = "special_throw";
        public string EffectFolder;
        public string ImpactFolder;
        public string CastSound = "Specials/power_hit";
        public string HitSound;
        public float Damage = 10f;
        public float Speed = 9f;
        public float Height = 2.5f;
        public int Startup = 12;
        public int Recovery = 20;
        public int Duration = 30;
        public int Hits = 1;
        public float EffectFps = 15f;
        public float EffectScale = 1f;
        public bool Returns;
        public HitLevel Level = HitLevel.Mid;
        public HitReaction Reaction = HitReaction.Knockdown;
        public Color Afterimage = Color.clear;
        public string Description;

        public static AbilityPreset Get(MKAbility ability)
        {
            return Presets.TryGetValue(ability, out AbilityPreset preset) ? preset : null;
        }

        public static IEnumerable<AbilityPreset> All => Presets.Values;

        private static readonly Dictionary<MKAbility, AbilityPreset> Presets = Build();

        private static Dictionary<MKAbility, AbilityPreset> Build()
        {
            List<AbilityPreset> list = new List<AbilityPreset>
            {
                new AbilityPreset
                {
                    Ability = MKAbility.Fireball, Kind = AbilityKind.Projectile, BodyAnimation = "special_lunge",
                    EffectFolder = "FX/liu_fireball", ImpactFolder = "FX/liu_fireball_impact", Damage = 10f,
                    Speed = 10f, Height = 2.6f, Reaction = HitReaction.Knockdown, Description = "Bola de fogo na altura do rosto."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.LowFireball, Kind = AbilityKind.Projectile, BodyAnimation = "crouch_punch",
                    EffectFolder = "FX/liu_fireball", ImpactFolder = "FX/liu_fireball_impact", Damage = 9f,
                    Speed = 10f, Height = 0.5f, Level = HitLevel.Low, Reaction = HitReaction.Knockdown,
                    Description = "Bola de fogo rente ao chão; precisa defender agachado."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.IceBall, Kind = AbilityKind.Projectile, BodyAnimation = "special_lunge",
                    EffectFolder = "FX/ice_blast", ImpactFolder = "FX/ice_break", Damage = 2f, Speed = 8f,
                    Height = 2.3f, Reaction = HitReaction.Freeze, CastSound = "Specials/ice_cast",
                    HitSound = "Specials/ice_hit", Recovery = 28, EffectScale = 1.1f,
                    Description = "Congela o oponente por alguns segundos."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.Spear, Kind = AbilityKind.Projectile, BodyAnimation = "special_throw",
                    EffectFolder = "FX/spear_head", Damage = 6f, Speed = 15f, Height = 2.6f,
                    Reaction = HitReaction.Pull, CastSound = "Specials/get_over_here", Recovery = 26,
                    Description = "Prende o oponente e puxa para perto, deixando-o tonto."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.ShadowBall, Kind = AbilityKind.Projectile, BodyAnimation = "special_lunge",
                    EffectFolder = "FX/shadow_ball", ImpactFolder = "FX/shadow_impact", Damage = 9f, Speed = 9f,
                    Height = 2.2f, Reaction = HitReaction.Stagger, EffectScale = 1.6f,
                    Description = "Bola verde em arco."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.EnergyWave, Kind = AbilityKind.Projectile, BodyAnimation = "high_punch",
                    EffectFolder = "FX/energy_wave", Damage = 10f, Speed = 9f, Height = 1.8f,
                    Reaction = HitReaction.Knockdown, Description = "Onda de energia disparada das mãos."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.Sai, Kind = AbilityKind.Projectile, BodyAnimation = "special_throw",
                    EffectFolder = "FX/sai", Damage = 8f, Speed = 13f, Height = 2.5f, Reaction = HitReaction.Stagger,
                    Description = "Arremessa sais em linha reta."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.FanThrow, Kind = AbilityKind.Projectile, BodyAnimation = "special_throw",
                    EffectFolder = "FX/fan_spin", Damage = 8f, Speed = 10f, Height = 2.5f, Returns = true,
                    Reaction = HitReaction.Stagger, Description = "Leque que vai e volta como bumerangue."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.Lightning, Kind = AbilityKind.Projectile, BodyAnimation = "special_lunge",
                    EffectFolder = "FX/green_spear", ImpactFolder = "FX/shadow_impact", Damage = 10f, Speed = 14f,
                    Height = 2.3f, Reaction = HitReaction.Knockdown, Description = "Raio horizontal."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.GreenSpear, Kind = AbilityKind.Projectile, BodyAnimation = "special_throw",
                    EffectFolder = "FX/green_spear", ImpactFolder = "FX/shadow_impact", Damage = 11f, Speed = 12f,
                    Height = 2.6f, Reaction = HitReaction.Knockdown, Description = "Lança de energia verde."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.TeleportPunch, Kind = AbilityKind.Teleport, BodyAnimation = "special_lunge",
                    EffectFolder = "FX/portal", Damage = 10f, Reaction = HitReaction.Knockdown, Startup = 10,
                    Description = "Some e reaparece atrás do oponente com um soco."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.TeleportKick, Kind = AbilityKind.Teleport, BodyAnimation = "flying_kick",
                    EffectFolder = "FX/portal", Damage = 10f, Reaction = HitReaction.Knockdown, Startup = 10,
                    Height = 2.6f, Description = "Some e cai do alto com uma voadora."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.Teleport, Kind = AbilityKind.Teleport, BodyAnimation = "crouch",
                    EffectFolder = "FX/portal", Damage = 0f, Startup = 8,
                    Description = "Some e reaparece do outro lado do oponente."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.Slide, Kind = AbilityKind.Dash, BodyAnimation = "sweep", Damage = 9f,
                    Speed = 11f, Height = 0f, Duration = 24, Level = HitLevel.Low, Reaction = HitReaction.Sweep,
                    Description = "Deslizada rente ao chão."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.ShadowKick, Kind = AbilityKind.Dash, BodyAnimation = "low_kick", Damage = 10f,
                    Speed = 14f, Height = 0f, Duration = 20, Reaction = HitReaction.Knockdown,
                    Afterimage = new Color(0.2f, 1f, 0.3f, 0.6f), Description = "Chute deslizante com rastro verde."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.FlyingKick, Kind = AbilityKind.Dash, BodyAnimation = "flying_kick",
                    Damage = 10f, Speed = 15f, Height = 1.2f, Duration = 22, Reaction = HitReaction.Knockdown,
                    Description = "Voadora horizontal atravessando a tela."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.BicycleKick, Kind = AbilityKind.Dash, BodyAnimation = "flying_kick",
                    Damage = 4f, Hits = 4, Speed = 7f, Height = 0.9f, Duration = 40, Reaction = HitReaction.Stagger,
                    Description = "Sequência de chutes em bicicleta."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.Torpedo, Kind = AbilityKind.Dash, BodyAnimation = "launched", Damage = 11f,
                    Speed = 16f, Height = 1.6f, Duration = 26, Reaction = HitReaction.Knockdown,
                    Afterimage = new Color(0.6f, 0.8f, 1f, 0.5f), Description = "Voa de cabeça e empurra o oponente."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.Roll, Kind = AbilityKind.Dash, BodyAnimation = "flip", Damage = 8f, Speed = 11f,
                    Height = 0f, Duration = 26, Level = HitLevel.Low, Reaction = HitReaction.Sweep,
                    Description = "Rola pelo chão derrubando o oponente."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.GroundPound, Kind = AbilityKind.GroundPound, BodyAnimation = "crouch_punch",
                    EffectFolder = "FX/energy_wave", Damage = 8f, Startup = 14, Recovery = 22,
                    Level = HitLevel.Unblockable, Reaction = HitReaction.Knockdown,
                    Description = "Soca o chão; derruba quem não estiver pulando."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.GotchaGrab, Kind = AbilityKind.Grab, BodyAnimation = "throw", Damage = 4f,
                    Hits = 4, Startup = 6, Recovery = 24, Level = HitLevel.Unblockable,
                    Description = "Agarra e dá uma série de socos."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.FanLift, Kind = AbilityKind.Lift, BodyAnimation = "special_throw",
                    EffectFolder = "FX/fan_arc", Damage = 2f, Startup = 12, Recovery = 20,
                    Reaction = HitReaction.Lift, Description = "Levanta o oponente no ar, indefeso."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.NutPunch, Kind = AbilityKind.NutPunch, BodyAnimation = "crouch_punch",
                    Damage = 4f, Startup = 6, Recovery = 18, Level = HitLevel.Low, Reaction = HitReaction.Dizzy,
                    Description = "Golpe baixo que deixa o oponente atordoado."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.UltimateBlast, Kind = AbilityKind.Ultimate, BodyAnimation = "special_throw",
                    EffectFolder = "FX/explosion", Damage = 30f, Startup = 30, Recovery = 30,
                    Level = HitLevel.Unblockable, Reaction = HitReaction.Knockdown,
                    Description = "Precisa da barra especial cheia. Tela azul e explosão."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.FatalityFire, Kind = AbilityKind.Fatality, BodyAnimation = "fatality_unmask",
                    EffectFolder = "FX/fire_ball", Description = "Cospe fogo e o oponente vira esqueleto."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.FatalityFreeze, Kind = AbilityKind.Fatality, BodyAnimation = "special_lunge",
                    EffectFolder = "FX/ice_blast", Description = "Congela e despedaça o oponente."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.FatalityExplode, Kind = AbilityKind.Fatality, BodyAnimation = "high_punch",
                    EffectFolder = "FX/explosion", Description = "O oponente explode em pedaços."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.FatalityUppercut, Kind = AbilityKind.Fatality, BodyAnimation = "uppercut",
                    EffectFolder = "FX/blood_fountain", Description = "Gancho que manda o oponente para o alto."
                },
                new AbilityPreset
                {
                    Ability = MKAbility.Babality, Kind = AbilityKind.Fatality, BodyAnimation = "victory_dance",
                    Description = "Transforma o oponente em bebê."
                }
            };

            Dictionary<MKAbility, AbilityPreset> map = new Dictionary<MKAbility, AbilityPreset>();
            foreach (AbilityPreset preset in list)
            {
                map[preset.Ability] = preset;
            }

            return map;
        }
    }
}
