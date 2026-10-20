using System.Collections.Generic;
using DevKombat.Fighters;
using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>
    /// Lista de lutadores da tela de seleção. Arraste aqui os FighterData dos amigos.
    /// O menu Dev Kombat > Preparar Kombat preenche automaticamente com todos os FighterData do projeto.
    /// </summary>
    [CreateAssetMenu(fileName = "KombatRoster", menuName = "Dev Kombat/Kombat Roster", order = 1)]
    public class KombatRoster : ScriptableObject
    {
        [Tooltip("Lutadores criados a partir das fichas dos amigos.")]
        public List<FighterData> fighters = new List<FighterData>();

        [Tooltip("Mostra também os modelos com as habilidades de cada personagem do MK2.")]
        public bool includeMkTemplates = true;

        public List<FighterData> BuildList()
        {
            List<FighterData> list = new List<FighterData>();
            foreach (FighterData fighter in fighters)
            {
                if (fighter != null && !list.Contains(fighter))
                {
                    list.Add(fighter);
                }
            }

            if (includeMkTemplates || list.Count < 2)
            {
                list.AddRange(KombatTemplates.Create());
            }

            return list;
        }
    }

    /// <summary>
    /// Modelos prontos com o conjunto de golpes de cada personagem do Mortal Kombat 2.
    /// Servem para testar o jogo e como ponto de partida: copie os golpes para a ficha do amigo.
    /// </summary>
    public static class KombatTemplates
    {
        private static List<FighterData> cache;

        public static List<FighterData> Create()
        {
            if (cache != null)
            {
                return cache;
            }

            cache = new List<FighterData>
            {
                FighterData.CreateRuntime(
                    "Ninja Amarelo", "Habilidades do Scorpion",
                    "Lança com corrente e teleporte. Modelo para um amigo estilo Scorpion.",
                    new Color(0.95f, 0.78f, 0.12f), false, FighterArchetype.Balanced,
                    new[]
                    {
                        new FighterSpecialMove("Lança (Get Over Here!)", "Trás, Trás + Soco Fraco", MKAbility.Spear, 6f, "Get over here!"),
                        new FighterSpecialMove("Teleporte com Soco", "Baixo, Trás + Soco Forte", MKAbility.TeleportPunch, 10f)
                    },
                    new FighterSpecialMove("Toasty!", "Cima, Cima + Soco Forte", MKAbility.FatalityFire, 0f),
                    new[] { "Venha cá!", "Vingança!" }),

                FighterData.CreateRuntime(
                    "Ninja Azul", "Habilidades do Sub-Zero",
                    "Gelo que congela e deslizada. Modelo para um amigo estilo Sub-Zero.",
                    new Color(0.25f, 0.55f, 1f), false, FighterArchetype.Zoner,
                    new[]
                    {
                        new FighterSpecialMove("Bola de Gelo", "Baixo, Frente + Soco Fraco", MKAbility.IceBall, 2f),
                        new FighterSpecialMove("Deslizada", "Trás + Soco Fraco + Chute Fraco", MKAbility.Slide, 9f)
                    },
                    new FighterSpecialMove("Congelar e Despedaçar", "Frente, Baixo, Frente + Soco Forte", MKAbility.FatalityFreeze, 0f),
                    new[] { "Frio como gelo.", "Você vai congelar." }),

                FighterData.CreateRuntime(
                    "Monge Dragão", "Habilidades do Liu Kang",
                    "Bolas de fogo, voadora e bicicleta. Modelo estilo Liu Kang.",
                    new Color(0.85f, 0.15f, 0.12f), false, FighterArchetype.Rushdown,
                    new[]
                    {
                        new FighterSpecialMove("Bola de Fogo", "Frente, Frente + Soco Forte", MKAbility.Fireball, 10f),
                        new FighterSpecialMove("Bola de Fogo Baixa", "Frente, Frente + Soco Fraco", MKAbility.LowFireball, 9f),
                        new FighterSpecialMove("Voadora", "Frente, Frente + Chute Forte", MKAbility.FlyingKick, 10f),
                        new FighterSpecialMove("Bicicleta", "Trás, Frente + Chute Fraco", MKAbility.BicycleKick, 4f)
                    },
                    new FighterSpecialMove("Gancho Final", "Frente, Baixo, Trás, Cima + Chute Forte", MKAbility.FatalityUppercut, 0f),
                    new[] { "Pelo templo!", "Kyaaa!" }),

                FighterData.CreateRuntime(
                    "Astro de Cinema", "Habilidades do Johnny Cage",
                    "Bola de sombra, chute sombra e golpe baixo. Modelo estilo Johnny Cage.",
                    new Color(0.2f, 0.85f, 0.3f), false, FighterArchetype.Balanced,
                    new[]
                    {
                        new FighterSpecialMove("Bola de Sombra", "Trás, Frente + Soco Fraco", MKAbility.ShadowBall, 9f),
                        new FighterSpecialMove("Chute Sombra", "Trás, Frente + Chute Fraco", MKAbility.ShadowKick, 10f),
                        new FighterSpecialMove("Golpe Baixo", "Baixo + Bloqueio + Soco Fraco", MKAbility.NutPunch, 4f, "Ooooh!")
                    },
                    new FighterSpecialMove("Gancho Hollywood", "Frente, Frente, Frente + Soco Forte", MKAbility.FatalityUppercut, 0f),
                    new[] { "Luz, câmera, porrada!", "Autógrafo depois." }),

                FighterData.CreateRuntime(
                    "Sargento", "Habilidades do Jax",
                    "Onda de energia, tremor no chão e agarrão. Modelo estilo Jax.",
                    new Color(0.65f, 0.65f, 0.7f), false, FighterArchetype.Grappler,
                    new[]
                    {
                        new FighterSpecialMove("Onda de Energia", "Frente, Baixo, Trás + Chute Forte", MKAbility.EnergyWave, 10f),
                        new FighterSpecialMove("Soco no Chão", "Baixo, Baixo + Chute Fraco", MKAbility.GroundPound, 8f),
                        new FighterSpecialMove("Agarrão", "Frente, Frente + Soco Fraco", MKAbility.GotchaGrab, 4f)
                    },
                    new FighterSpecialMove("Explosão", "Bloqueio, Bloqueio, Bloqueio + Soco Fraco", MKAbility.FatalityExplode, 0f),
                    new[] { "Na posição, soldado!", "Missão cumprida." }),

                FighterData.CreateRuntime(
                    "Princesa do Leque", "Habilidades da Kitana/Jade",
                    "Leque bumerangue e leque que levanta. Modelo estilo Kitana.",
                    new Color(0.35f, 0.4f, 1f), true, FighterArchetype.Zoner,
                    new[]
                    {
                        new FighterSpecialMove("Leque Bumerangue", "Frente, Frente + Soco Forte", MKAbility.FanThrow, 8f),
                        new FighterSpecialMove("Leque que Levanta", "Trás, Trás, Trás + Soco Forte", MKAbility.FanLift, 2f)
                    },
                    new FighterSpecialMove("Beijo Explosivo", "Baixo, Baixo, Frente + Chute Forte", MKAbility.FatalityExplode, 0f),
                    new[] { "Pelo trono de Edenia.", "Que elegante." }),

                FighterData.CreateRuntime(
                    "Assassina de Sai", "Habilidades da Mileena",
                    "Sais, teleporte com chute e rolamento. Modelo estilo Mileena.",
                    new Color(0.85f, 0.25f, 0.85f), true, FighterArchetype.Rushdown,
                    new[]
                    {
                        new FighterSpecialMove("Sais", "Trás, Frente + Soco Forte", MKAbility.Sai, 8f),
                        new FighterSpecialMove("Teleporte com Chute", "Frente, Frente + Chute Fraco", MKAbility.TeleportKick, 10f),
                        new FighterSpecialMove("Rolamento", "Trás, Trás, Baixo + Chute Forte", MKAbility.Roll, 8f)
                    },
                    new FighterSpecialMove("Mordida Explosiva", "Frente, Trás, Frente + Soco Fraco", MKAbility.FatalityExplode, 0f),
                    new[] { "Hihihi.", "Você é meu almoço." }),

                FighterData.CreateRuntime(
                    "Deus do Trovão", "Habilidades do Raiden",
                    "Raio, torpedo e teleporte. Modelo estilo Raiden.",
                    new Color(0.9f, 0.9f, 1f), false, FighterArchetype.Balanced,
                    new[]
                    {
                        new FighterSpecialMove("Raio", "Baixo, Frente + Soco Fraco", MKAbility.Lightning, 10f),
                        new FighterSpecialMove("Torpedo", "Trás, Trás, Frente", MKAbility.Torpedo, 11f),
                        new FighterSpecialMove("Teleporte", "Baixo, Cima", MKAbility.Teleport, 0f)
                    },
                    new FighterSpecialMove("Sobrecarga", "Frente, Trás, Trás + Soco Forte", MKAbility.FatalityExplode, 0f),
                    new[] { "O trovão chegou.", "Pelo Plano Terreno!" })
            };

            return cache;
        }
    }
}
