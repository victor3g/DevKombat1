using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>Fonte de comandos de um lutador: teclado, CPU ou boneco de treino.</summary>
    public interface IKombatInputSource
    {
        KombatInputFrame Read(KombatFighter self, KombatFighter opponent);
    }

    /// <summary>Teclas de um jogador. Os botões seguem o MK2: dois socos, dois chutes e bloqueio.</summary>
    [System.Serializable]
    public class KombatKeyMap
    {
        public KeyCode up = KeyCode.W;
        public KeyCode down = KeyCode.S;
        public KeyCode left = KeyCode.A;
        public KeyCode right = KeyCode.D;
        public KeyCode lowPunch = KeyCode.J;
        public KeyCode highPunch = KeyCode.U;
        public KeyCode lowKick = KeyCode.K;
        public KeyCode highKick = KeyCode.I;
        public KeyCode block = KeyCode.L;
        public KeyCode altBlock = KeyCode.Space;

        public static KombatKeyMap PlayerOne() => new KombatKeyMap();

        public static KombatKeyMap PlayerTwo() => new KombatKeyMap
        {
            up = KeyCode.UpArrow,
            down = KeyCode.DownArrow,
            left = KeyCode.LeftArrow,
            right = KeyCode.RightArrow,
            lowPunch = KeyCode.Keypad1,
            highPunch = KeyCode.Keypad4,
            lowKick = KeyCode.Keypad2,
            highKick = KeyCode.Keypad5,
            block = KeyCode.Keypad3,
            altBlock = KeyCode.Keypad0
        };

        public string Describe()
        {
            return $"{up}/{left}/{down}/{right} mover · {lowPunch} soco fraco · {highPunch} soco forte · " +
                   $"{lowKick} chute fraco · {highKick} chute forte · {block} defesa";
        }
    }

    public class KeyboardKombatInput : IKombatInputSource
    {
        private readonly KombatKeyMap keys;
        private KombatButtons pending;

        public KeyboardKombatInput(KombatKeyMap keyMap)
        {
            keys = keyMap;
        }

        /// <summary>
        /// Guarda os botões apertados neste quadro de tela. A lógica roda em quadros fixos
        /// de 60 Hz, que podem não coincidir com os da tela; sem isso, toques se perderiam.
        /// </summary>
        public void Poll()
        {
            Latch(keys.lowPunch, KombatButtons.LowPunch);
            Latch(keys.highPunch, KombatButtons.HighPunch);
            Latch(keys.lowKick, KombatButtons.LowKick);
            Latch(keys.highKick, KombatButtons.HighKick);
            Latch(keys.block, KombatButtons.Block);
            Latch(keys.altBlock, KombatButtons.Block);
        }

        private void Latch(KeyCode key, KombatButtons button)
        {
            if (key != KeyCode.None && Input.GetKeyDown(key))
            {
                pending |= button;
            }
        }

        public KombatInputFrame Read(KombatFighter self, KombatFighter opponent)
        {
            KombatInputFrame frame = new KombatInputFrame();
            if (Input.GetKey(keys.left)) frame.Horizontal -= 1;
            if (Input.GetKey(keys.right)) frame.Horizontal += 1;
            if (Input.GetKey(keys.up)) frame.Vertical += 1;
            if (Input.GetKey(keys.down)) frame.Vertical -= 1;

            Add(ref frame, keys.lowPunch, KombatButtons.LowPunch);
            Add(ref frame, keys.highPunch, KombatButtons.HighPunch);
            Add(ref frame, keys.lowKick, KombatButtons.LowKick);
            Add(ref frame, keys.highKick, KombatButtons.HighKick);
            Add(ref frame, keys.block, KombatButtons.Block);
            Add(ref frame, keys.altBlock, KombatButtons.Block);
            frame.Pressed |= pending;
            frame.Held |= pending;
            pending = KombatButtons.None;
            return frame;
        }

        private static void Add(ref KombatInputFrame frame, KeyCode key, KombatButtons button)
        {
            if (key == KeyCode.None)
            {
                return;
            }

            if (Input.GetKey(key)) frame.Held |= button;
        }
    }

    /// <summary>Boneco do modo treino: fica parado e, se pedido, defende tudo.</summary>
    public class DummyKombatInput : IKombatInputSource
    {
        public bool AlwaysBlock;

        public KombatInputFrame Read(KombatFighter self, KombatFighter opponent)
        {
            return new KombatInputFrame { Held = AlwaysBlock ? KombatButtons.Block : KombatButtons.None };
        }
    }

    /// <summary>
    /// Guarda os últimos quadros de controle (em direções relativas ao lado do lutador)
    /// para reconhecer comandos como "Baixo, Frente + Soco".
    /// </summary>
    public class KombatInputBuffer
    {
        public const int Size = 45;

        private readonly KombatDirection[] directions = new KombatDirection[Size];
        private readonly KombatButtons[] pressed = new KombatButtons[Size];
        private readonly KombatButtons[] held = new KombatButtons[Size];
        private int head;

        public void Push(KombatInputFrame frame, int facing)
        {
            head = (head + 1) % Size;
            directions[head] = frame.Relative(facing);
            pressed[head] = frame.Pressed;
            held[head] = frame.Held;
        }

        public void Clear()
        {
            for (int i = 0; i < Size; i++)
            {
                directions[i] = KombatDirection.None;
                pressed[i] = KombatButtons.None;
                held[i] = KombatButtons.None;
            }
        }

        /// <summary>Quadro de "ago" quadros atrás (0 = atual).</summary>
        public KombatDirection DirectionAt(int ago) => directions[Index(ago)];
        public KombatButtons PressedAt(int ago) => pressed[Index(ago)];
        public KombatButtons HeldAt(int ago) => held[Index(ago)];

        private int Index(int ago) => ((head - ago) % Size + Size) % Size;

        public bool Matches(KombatCommand command)
        {
            if (command == null || !command.IsValid)
            {
                return false;
            }

            List<KombatCommand.Step> steps = command.Steps;
            KombatCommand.Step last = steps[steps.Count - 1];

            // O último passo precisa acontecer agora.
            if (!StepNow(last, 0, out int lastFrame))
            {
                return false;
            }

            int cursor = SkipHold(last, lastFrame);
            for (int s = steps.Count - 2; s >= 0; s--)
            {
                KombatCommand.Step step = steps[s];
                bool found = false;
                int limit = Mathf.Min(Size - 1, cursor + KombatCommand.MaxGapFrames);
                for (int ago = cursor + 1; ago <= limit; ago++)
                {
                    if (StepAt(step, ago))
                    {
                        // Pula o tempo em que a direção ficou segura, para que
                        // direções repetidas (Trás, Trás) precisem ser apertadas duas vezes.
                        cursor = SkipHold(step, ago);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private int SkipHold(KombatCommand.Step step, int ago)
        {
            if (step.Directions == KombatDirection.None)
            {
                return ago;
            }

            while (ago + 1 < Size && HasDirections(DirectionAt(ago + 1), step.Directions))
            {
                ago++;
            }

            return ago;
        }

        private bool StepNow(KombatCommand.Step step, int ago, out int frameIndex)
        {
            frameIndex = ago;
            if (step.Buttons == KombatButtons.None)
            {
                // Comando só de direções: dispara no quadro em que a última direção começa.
                return HasDirections(DirectionAt(0), step.Directions) &&
                       !HasDirections(DirectionAt(1), step.Directions);
            }

            // Botões: todos seguros agora, ao menos um apertado nos últimos 3 quadros.
            if (!HasAll(HeldAt(0), step.Buttons) || !HasDirections(DirectionAt(0), step.Directions))
            {
                return false;
            }

            for (int i = 0; i < 3; i++)
            {
                if (Overlaps(PressedAt(i), step.Buttons))
                {
                    frameIndex = i;
                    return i == 0 || Overlaps(PressedAt(0), step.Buttons);
                }
            }

            return false;
        }

        private bool StepAt(KombatCommand.Step step, int ago)
        {
            if (step.Buttons != KombatButtons.None)
            {
                return Overlaps(PressedAt(ago), step.Buttons) && HasDirections(DirectionAt(ago), step.Directions);
            }

            return HasDirections(DirectionAt(ago), step.Directions);
        }

        private static bool HasDirections(KombatDirection current, KombatDirection wanted)
        {
            return (current & wanted) == wanted;
        }

        private static bool HasAll(KombatButtons current, KombatButtons wanted)
        {
            // "Soco" aceita qualquer soco, "Chute" qualquer chute.
            if ((wanted & KombatButtons.AnyPunch) == KombatButtons.AnyPunch)
            {
                if ((current & KombatButtons.AnyPunch) == 0) return false;
                wanted &= ~KombatButtons.AnyPunch;
            }

            if ((wanted & KombatButtons.AnyKick) == KombatButtons.AnyKick)
            {
                if ((current & KombatButtons.AnyKick) == 0) return false;
                wanted &= ~KombatButtons.AnyKick;
            }

            return (current & wanted) == wanted;
        }

        private static bool Overlaps(KombatButtons current, KombatButtons wanted)
        {
            KombatButtons attack = wanted & ~KombatButtons.Block;
            return attack != KombatButtons.None ? (current & attack) != 0 : (current & wanted) != 0;
        }
    }

    /// <summary>
    /// Comando de golpe especial escrito em português ou inglês.
    /// Exemplos: "Baixo, Frente + Soco", "Trás, Trás + Soco Fraco", "D, F + HP".
    /// </summary>
    public class KombatCommand
    {
        public const int MaxGapFrames = 14;

        public struct Step
        {
            public KombatDirection Directions;
            public KombatButtons Buttons;
        }

        private static readonly Dictionary<string, KombatCommand> Cache = new Dictionary<string, KombatCommand>();

        public readonly List<Step> Steps = new List<Step>();
        public readonly string Text;

        private KombatCommand(string text)
        {
            Text = text;
        }

        public bool IsValid => Steps.Count > 0 && Steps[Steps.Count - 1].Buttons != KombatButtons.None ||
                               Steps.Count > 1;

        /// <summary>Número de passos; comandos mais longos têm prioridade.</summary>
        public int Complexity => Steps.Count * 10 + CountBits((int)Steps[Steps.Count - 1].Buttons);

        public static KombatCommand Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (Cache.TryGetValue(text, out KombatCommand cached))
            {
                return cached;
            }

            KombatCommand command = new KombatCommand(text);
            foreach (string rawStep in text.Split(','))
            {
                Step step = new Step();
                foreach (string rawToken in rawStep.Split('+'))
                {
                    string token = Normalize(rawToken);
                    if (token.Length == 0) continue;
                    if (!TryDirection(token, ref step.Directions) && !TryButton(token, ref step.Buttons))
                    {
                        Debug.LogWarning($"Dev Kombat: parte do comando não reconhecida: \"{rawToken.Trim()}\" em \"{text}\".");
                    }
                }

                if (step.Directions != KombatDirection.None || step.Buttons != KombatButtons.None)
                {
                    command.Steps.Add(step);
                }
            }

            Cache[text] = command;
            return command;
        }

        /// <summary>Gera a sequência de quadros que executa o comando (usada pela CPU).</summary>
        public List<KombatInputFrame> ToFrames(int facing)
        {
            List<KombatInputFrame> frames = new List<KombatInputFrame>();
            foreach (Step step in Steps)
            {
                KombatInputFrame frame = new KombatInputFrame();
                if ((step.Directions & KombatDirection.Up) != 0) frame.Vertical = 1;
                if ((step.Directions & KombatDirection.Down) != 0) frame.Vertical = -1;
                if ((step.Directions & KombatDirection.Forward) != 0) frame.Horizontal = facing;
                if ((step.Directions & KombatDirection.Back) != 0) frame.Horizontal = -facing;

                KombatButtons buttons = step.Buttons;
                if ((buttons & KombatButtons.AnyPunch) == KombatButtons.AnyPunch) buttons &= ~KombatButtons.LowPunch;
                if ((buttons & KombatButtons.AnyKick) == KombatButtons.AnyKick) buttons &= ~KombatButtons.LowKick;
                frame.Held = buttons;
                frame.Pressed = buttons;

                // Cada direção fica dois quadros e é solta antes da próxima.
                frames.Add(frame);
                if (buttons == KombatButtons.None)
                {
                    frames.Add(frame);
                    frames.Add(new KombatInputFrame());
                }
            }

            return frames;
        }

        private static bool TryDirection(string token, ref KombatDirection direction)
        {
            switch (token)
            {
                case "baixo": case "down": case "d": case "b↓": case "↓":
                    direction |= KombatDirection.Down; return true;
                case "cima": case "up": case "u": case "↑":
                    direction |= KombatDirection.Up; return true;
                case "frente": case "forward": case "f": case "→":
                    direction |= KombatDirection.Forward; return true;
                case "tras": case "back": case "b": case "←": case "atras":
                    direction |= KombatDirection.Back; return true;
                case "baixo-frente": case "df": case "baixo frente":
                    direction |= KombatDirection.Down | KombatDirection.Forward; return true;
                case "baixo-tras": case "db": case "baixo tras":
                    direction |= KombatDirection.Down | KombatDirection.Back; return true;
                default:
                    return false;
            }
        }

        private static bool TryButton(string token, ref KombatButtons buttons)
        {
            switch (token)
            {
                case "soco fraco": case "lp": case "low punch":
                    buttons |= KombatButtons.LowPunch; return true;
                case "soco forte": case "hp": case "high punch":
                    buttons |= KombatButtons.HighPunch; return true;
                case "soco": case "punch": case "p":
                    buttons |= KombatButtons.AnyPunch; return true;
                case "chute fraco": case "lk": case "low kick":
                    buttons |= KombatButtons.LowKick; return true;
                case "chute forte": case "hk": case "high kick":
                    buttons |= KombatButtons.HighKick; return true;
                case "chute": case "kick": case "k":
                    buttons |= KombatButtons.AnyKick; return true;
                case "bloqueio": case "defesa": case "block": case "bl":
                    buttons |= KombatButtons.Block; return true;
                default:
                    return false;
            }
        }

        private static string Normalize(string token)
        {
            string lower = token.Trim().ToLowerInvariant();
            string decomposed = lower.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new StringBuilder(decomposed.Length);
            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            string text = builder.ToString();
            while (text.Contains("  ")) text = text.Replace("  ", " ");
            return text;
        }

        private static int CountBits(int value)
        {
            int count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }
    }
}
