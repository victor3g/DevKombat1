using System.Collections.Generic;
using DevKombat.Fighters;
using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>
    /// Sprites próprios de um lutador. Cada lista aceita um ou vários quadros.
    /// Listas vazias usam outra animação parecida e, por último, o ninja do MK2 tingido.
    /// </summary>
    [System.Serializable]
    public class FighterSpriteSet
    {
        [Tooltip("Parado em guarda.")] public Sprite[] idle;
        [Tooltip("Andando.")] public Sprite[] walk;
        [Tooltip("Agachado.")] public Sprite[] crouch;
        [Tooltip("Pulando.")] public Sprite[] jump;
        [Tooltip("Defendendo em pé.")] public Sprite[] block;
        [Tooltip("Socos em pé, agachado e no ar.")] public Sprite[] punch;
        [Tooltip("Chutes.")] public Sprite[] kick;
        [Tooltip("Gancho (Baixo + Soco Forte).")] public Sprite[] uppercut;
        [Tooltip("Rasteira (Trás + Chute Fraco).")] public Sprite[] sweep;
        [Tooltip("Arremessando um projétil ou usando especial.")] public Sprite[] special;
        [Tooltip("Agarrão/arremesso.")] public Sprite[] grab;
        [Tooltip("Recebendo dano.")] public Sprite[] hit;
        [Tooltip("Caindo e no chão.")] public Sprite[] knockdown;
        [Tooltip("Tonto (FINISH HIM/HER).")] public Sprite[] dizzy;
        [Tooltip("Vitória.")] public Sprite[] victory;
        [Tooltip("Derrota.")] public Sprite[] defeat;
        [Tooltip("Provocação ou entrada.")] public Sprite[] taunt;

        public bool HasAny => idle != null && idle.Length > 0;
    }

    /// <summary>Quadros prontos de uma animação: corpo e (opcional) roupa tingível.</summary>
    public class KombatFrames
    {
        public Sprite[] Body;
        public Sprite[] Costume;
        public bool Custom;
        public int Count => Body != null ? Body.Length : 0;
    }

    /// <summary>
    /// Resolve as animações de um lutador, misturando os sprites próprios do amigo
    /// com o ninja do MK2 para tudo o que ainda não foi desenhado.
    /// </summary>
    public class KombatAnimationLibrary
    {
        // Animação do ninja -> lista de campos próprios que podem substituí-la, em ordem.
        private static readonly Dictionary<string, string[]> CustomFallbacks = new Dictionary<string, string[]>
        {
            { "idle", new[] { "idle" } },
            { "walk", new[] { "walk", "idle" } },
            { "crouch", new[] { "crouch", "idle" } },
            { "crouch_block", new[] { "crouch", "block", "idle" } },
            { "block", new[] { "block", "idle" } },
            { "jump_start", new[] { "jump", "idle" } },
            { "jump_up", new[] { "jump", "idle" } },
            { "jump_land", new[] { "crouch", "jump", "idle" } },
            { "flip", new[] { "jump", "idle" } },
            { "low_punch", new[] { "punch", "idle" } },
            { "high_punch", new[] { "punch", "idle" } },
            { "crouch_punch", new[] { "punch", "crouch", "idle" } },
            { "jump_punch", new[] { "punch", "jump", "idle" } },
            { "uppercut", new[] { "uppercut", "punch", "idle" } },
            { "low_kick", new[] { "kick", "idle" } },
            { "high_kick", new[] { "kick", "idle" } },
            { "roundhouse", new[] { "kick", "idle" } },
            { "knee", new[] { "kick", "idle" } },
            { "crouch_kick", new[] { "sweep", "kick", "idle" } },
            { "sweep", new[] { "sweep", "kick", "idle" } },
            { "jump_kick", new[] { "kick", "jump", "idle" } },
            { "flying_kick", new[] { "kick", "jump", "idle" } },
            { "throw", new[] { "grab", "punch", "idle" } },
            { "thrown", new[] { "knockdown", "hit", "idle" } },
            { "special_throw", new[] { "special", "punch", "idle" } },
            { "special_lunge", new[] { "special", "punch", "idle" } },
            { "hit_high", new[] { "hit", "idle" } },
            { "hit_low", new[] { "hit", "idle" } },
            { "crouch_hit", new[] { "hit", "crouch", "idle" } },
            { "knockdown", new[] { "knockdown", "defeat", "hit", "idle" } },
            { "launched", new[] { "knockdown", "defeat", "hit", "idle" } },
            { "get_up", new[] { "crouch", "idle" } },
            { "dizzy", new[] { "dizzy", "hit", "idle" } },
            { "victory", new[] { "victory", "idle" } },
            { "victory_dance", new[] { "taunt", "victory", "idle" } },
            { "defeat", new[] { "defeat", "knockdown", "hit", "idle" } },
            { "fatality_unmask", new[] { "special", "taunt", "idle" } },
            { "baby", new[] { "idle" } }
        };

        private readonly Dictionary<string, KombatFrames> cache = new Dictionary<string, KombatFrames>();
        private readonly FighterSpriteSet custom;
        private readonly bool useCustom;

        public KombatAnimationLibrary(FighterData data)
        {
            custom = data != null ? data.Sprites : null;
            useCustom = custom != null && custom.HasAny;
            HasCustomSprites = useCustom;
        }

        public bool HasCustomSprites { get; }

        public KombatFrames Get(string key)
        {
            if (cache.TryGetValue(key, out KombatFrames frames))
            {
                return frames;
            }

            frames = useCustom ? ResolveCustom(key) : null;
            if (frames == null)
            {
                frames = new KombatFrames
                {
                    Body = KombatResources.Frames("Fighters/Ninja/" + key),
                    Costume = KombatResources.Frames("Fighters/NinjaCostume/" + key)
                };
            }

            if (frames.Count == 0 && key != "idle")
            {
                frames = Get("idle");
            }

            cache[key] = frames;
            return frames;
        }

        private KombatFrames ResolveCustom(string key)
        {
            if (!CustomFallbacks.TryGetValue(key, out string[] options))
            {
                options = new[] { "idle" };
            }

            foreach (string option in options)
            {
                Sprite[] sprites = Field(option);
                if (sprites != null && sprites.Length > 0)
                {
                    return new KombatFrames { Body = sprites, Custom = true };
                }
            }

            return null;
        }

        private Sprite[] Field(string name)
        {
            switch (name)
            {
                case "idle": return custom.idle;
                case "walk": return custom.walk;
                case "crouch": return custom.crouch;
                case "jump": return custom.jump;
                case "block": return custom.block;
                case "punch": return custom.punch;
                case "kick": return custom.kick;
                case "uppercut": return custom.uppercut;
                case "sweep": return custom.sweep;
                case "special": return custom.special;
                case "grab": return custom.grab;
                case "hit": return custom.hit;
                case "knockdown": return custom.knockdown;
                case "dizzy": return custom.dizzy;
                case "victory": return custom.victory;
                case "defeat": return custom.defeat;
                case "taunt": return custom.taunt;
                default: return null;
            }
        }
    }
}
