using System;
using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>
    /// Habilidades do Mortal Kombat 2 que podem ser dadas a qualquer lutador.
    /// Cada personagem escolhe as suas e muda nome, comando, dano e aparência.
    /// </summary>
    public enum MKAbility
    {
        [InspectorName("Nenhuma")] None = 0,

        [InspectorName("Projétil/Bola de fogo alta (Liu Kang)")] Fireball = 10,
        [InspectorName("Projétil/Bola de fogo baixa (Liu Kang)")] LowFireball = 11,
        [InspectorName("Projétil/Bola de gelo que congela (Sub-Zero)")] IceBall = 12,
        [InspectorName("Projétil/Lança que puxa - Get over here! (Scorpion)")] Spear = 13,
        [InspectorName("Projétil/Bola de sombra (Johnny Cage)")] ShadowBall = 14,
        [InspectorName("Projétil/Onda de energia (Jax)")] EnergyWave = 15,
        [InspectorName("Projétil/Sai (Mileena)")] Sai = 16,
        [InspectorName("Projétil/Leque bumerangue (Kitana/Jade)")] FanThrow = 17,
        [InspectorName("Projétil/Raio (Raiden)")] Lightning = 18,
        [InspectorName("Projétil/Lança verde (Shao Kahn)")] GreenSpear = 19,

        [InspectorName("Teleporte/Teleporte com soco (Scorpion)")] TeleportPunch = 30,
        [InspectorName("Teleporte/Teleporte com chute aéreo (Mileena)")] TeleportKick = 31,
        [InspectorName("Teleporte/Teleporte simples (Raiden)")] Teleport = 32,

        [InspectorName("Investida/Deslizada (Sub-Zero)")] Slide = 50,
        [InspectorName("Investida/Chute sombra (Johnny Cage)")] ShadowKick = 51,
        [InspectorName("Investida/Voadora (Liu Kang)")] FlyingKick = 52,
        [InspectorName("Investida/Bicicleta (Liu Kang)")] BicycleKick = 53,
        [InspectorName("Investida/Torpedo (Raiden)")] Torpedo = 54,
        [InspectorName("Investida/Rolamento (Mileena)")] Roll = 55,

        [InspectorName("Corpo a corpo/Soco no chão - tremor (Jax)")] GroundPound = 70,
        [InspectorName("Corpo a corpo/Agarrão com socos (Jax)")] GotchaGrab = 71,
        [InspectorName("Corpo a corpo/Leque que levanta (Kitana)")] FanLift = 72,
        [InspectorName("Corpo a corpo/Golpe baixo que atordoa (Johnny Cage)")] NutPunch = 73,

        [InspectorName("Supremo/Explosão com tela azul")] UltimateBlast = 90,

        [InspectorName("Fatality/Fogo e esqueleto (Scorpion)")] FatalityFire = 100,
        [InspectorName("Fatality/Congelar e despedaçar (Sub-Zero)")] FatalityFreeze = 101,
        [InspectorName("Fatality/Explodir em pedaços (Kano/Jax)")] FatalityExplode = 102,
        [InspectorName("Fatality/Gancho com chuva de sangue")] FatalityUppercut = 103,
        [InspectorName("Fatality/Babality")] Babality = 104
    }

    [Flags]
    public enum KombatButtons
    {
        None = 0,
        LowPunch = 1,
        HighPunch = 2,
        LowKick = 4,
        HighKick = 8,
        Block = 16,
        Start = 32,
        AnyPunch = LowPunch | HighPunch,
        AnyKick = LowKick | HighKick
    }

    /// <summary>Direções relativas ao lado para o qual o lutador está virado.</summary>
    [Flags]
    public enum KombatDirection
    {
        None = 0,
        Up = 1,
        Down = 2,
        Forward = 4,
        Back = 8
    }

    public enum HitLevel
    {
        High,
        Mid,
        Low,
        Overhead,
        Unblockable
    }

    public enum HitReaction
    {
        Stagger,
        Knockdown,
        Sweep,
        Launch,
        Freeze,
        Pull,
        Lift,
        Dizzy
    }

    /// <summary>Estado dos controles em um quadro, já em coordenadas absolutas.</summary>
    public struct KombatInputFrame
    {
        public int Horizontal;
        public int Vertical;
        public KombatButtons Held;
        public KombatButtons Pressed;

        public KombatDirection Relative(int facing)
        {
            KombatDirection direction = KombatDirection.None;
            if (Vertical > 0) direction |= KombatDirection.Up;
            if (Vertical < 0) direction |= KombatDirection.Down;
            if (Horizontal != 0)
            {
                direction |= Horizontal == facing ? KombatDirection.Forward : KombatDirection.Back;
            }

            return direction;
        }

        public bool IsPressed(KombatButtons buttons) => (Pressed & buttons) != 0;
        public bool IsHeld(KombatButtons buttons) => (Held & buttons) != 0;
    }

    /// <summary>Retângulo de colisão no espaço do mundo.</summary>
    public struct KombatBox
    {
        public float XMin;
        public float YMin;
        public float XMax;
        public float YMax;

        public KombatBox(float xMin, float yMin, float xMax, float yMax)
        {
            XMin = Mathf.Min(xMin, xMax);
            XMax = Mathf.Max(xMin, xMax);
            YMin = Mathf.Min(yMin, yMax);
            YMax = Mathf.Max(yMin, yMax);
        }

        public bool Overlaps(KombatBox other)
        {
            return XMin < other.XMax && XMax > other.XMin && YMin < other.YMax && YMax > other.YMin;
        }

        public Vector2 Center => new Vector2((XMin + XMax) * 0.5f, (YMin + YMax) * 0.5f);
    }
}
