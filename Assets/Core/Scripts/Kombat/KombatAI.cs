using System.Collections.Generic;
using DevKombat.Fighters;
using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>
    /// CPU no estilo MK2: pressiona, defende, usa gancho contra pulos,
    /// arremessa projéteis de longe e tenta fazer o Fatality no fim.
    /// Ela usa os mesmos controles de um jogador, inclusive os comandos especiais.
    /// </summary>
    public class KombatAI : IKombatInputSource
    {
        public float Difficulty;

        private readonly Queue<KombatInputFrame> plan = new Queue<KombatInputFrame>();
        private int holdFrames;
        private KombatInputFrame holding;
        private int reaction;
        private int wait;
        private KombatFighter self;

        public KombatAI(float difficulty = 0.5f)
        {
            Difficulty = Mathf.Clamp01(difficulty);
        }

        public KombatInputFrame Read(KombatFighter me, KombatFighter opponent)
        {
            self = me;
            if (plan.Count > 0)
            {
                return plan.Dequeue();
            }

            if (holdFrames > 0)
            {
                holdFrames--;
                return holding;
            }

            if (me.Match == null || !me.Match.FightActive && !me.Match.FinishActive)
            {
                return new KombatInputFrame();
            }

            if (wait > 0)
            {
                wait--;
                return Defensive(me, opponent);
            }

            Decide(me, opponent);
            return plan.Count > 0 ? plan.Dequeue() : holding;
        }

        private KombatInputFrame Defensive(KombatFighter me, KombatFighter opponent)
        {
            // Mesmo esperando, defende se o golpe já estiver vindo.
            float distance = Mathf.Abs(opponent.Position.x - me.Position.x);
            bool threat = opponent.IsAttacking && distance < 2.8f || me.Match.ProjectileThreat(me) < 4f;
            if (threat && Random.value < 0.25f + Difficulty * 0.6f)
            {
                bool low = opponent.CurrentMove != null && opponent.CurrentMove.Level == HitLevel.Low;
                return new KombatInputFrame { Held = KombatButtons.Block, Vertical = low ? -1 : 0 };
            }

            return new KombatInputFrame();
        }

        private void Decide(KombatFighter me, KombatFighter opponent)
        {
            int facing = me.Facing;
            float distance = Mathf.Abs(opponent.Position.x - me.Position.x);
            reaction = Mathf.RoundToInt(Mathf.Lerp(22f, 5f, Difficulty));
            wait = Random.Range(reaction / 2, reaction);

            if (me.Match.FinishActive)
            {
                DecideFinisher(me, opponent, distance);
                return;
            }

            if (me.State == FighterState.Jump || me.IsDown)
            {
                return;
            }

            float projectile = me.Match.ProjectileThreat(me);
            if (projectile < 4.5f && Random.value < 0.35f + Difficulty * 0.5f)
            {
                if (Random.value < 0.3f)
                {
                    Hold(new KombatInputFrame { Vertical = 1, Horizontal = facing }, 4);
                }
                else
                {
                    Hold(new KombatInputFrame { Held = KombatButtons.Block }, 24);
                }

                return;
            }

            if (opponent.IsAttacking && distance < 2.6f && Random.value < 0.2f + Difficulty * 0.6f)
            {
                bool low = opponent.CurrentMove != null && opponent.CurrentMove.Level == HitLevel.Low;
                Hold(new KombatInputFrame { Held = KombatButtons.Block, Vertical = low ? -1 : 0 }, 18);
                return;
            }

            if (opponent.State == FighterState.Jump && distance < 2.6f && Random.value < 0.25f + Difficulty * 0.6f)
            {
                Press(KombatButtons.HighPunch, -1, 0);
                return;
            }

            if (opponent.State == FighterState.Dizzy || opponent.State == FighterState.Frozen)
            {
                Walk(facing, distance > 1.4f ? 10 : 0);
                Press(distance < 1.8f ? KombatButtons.HighPunch : KombatButtons.HighKick, distance < 1.8f ? -1 : 0, 0);
                return;
            }

            if (distance > 4.2f)
            {
                if (TrySpecial(me, AbilityKind.Projectile, 0.45f) || TrySpecial(me, AbilityKind.Dash, 0.2f) ||
                    TrySpecial(me, AbilityKind.Teleport, 0.12f))
                {
                    return;
                }

                if (Random.value < 0.15f)
                {
                    Hold(new KombatInputFrame { Vertical = 1, Horizontal = facing }, 4);
                    plan.Enqueue(new KombatInputFrame());
                    return;
                }

                Walk(facing, Random.Range(14, 30));
                return;
            }

            if (distance > 1.9f)
            {
                float roll = Random.value;
                if (roll < 0.15f && TrySpecial(me, AbilityKind.Dash, 1f)) return;
                if (roll < 0.25f && TrySpecial(me, AbilityKind.Teleport, 1f)) return;
                if (roll < 0.35f && TrySpecial(me, AbilityKind.Projectile, 1f)) return;
                if (roll < 0.5f)
                {
                    // Pulo com voadora.
                    Hold(new KombatInputFrame { Vertical = 1, Horizontal = facing }, 3);
                    for (int i = 0; i < 14; i++) plan.Enqueue(new KombatInputFrame());
                    plan.Enqueue(new KombatInputFrame { Pressed = KombatButtons.HighKick, Held = KombatButtons.HighKick });
                    return;
                }

                if (roll < 0.62f && distance < 2.3f)
                {
                    Press(KombatButtons.LowKick, 0, -facing);
                    return;
                }

                Walk(facing, Random.Range(10, 22));
                return;
            }

            // Perto.
            float choice = Random.value;
            if (choice < 0.12f && opponent.IsThrowable)
            {
                Press(KombatButtons.LowPunch, 0, facing);
            }
            else if (choice < 0.24f)
            {
                Press(KombatButtons.HighPunch, -1, 0);
            }
            else if (choice < 0.36f)
            {
                Press(KombatButtons.LowKick, 0, -facing);
            }
            else if (choice < 0.46f)
            {
                Press(KombatButtons.HighKick, 0, -facing);
            }
            else if (choice < 0.56f && TrySpecial(me, AbilityKind.Grab, 1f) || choice < 0.6f && TrySpecial(me, AbilityKind.NutPunch, 1f))
            {
            }
            else if (choice < 0.7f)
            {
                Combo(KombatButtons.HighPunch, KombatButtons.HighPunch, KombatButtons.LowPunch);
            }
            else if (choice < 0.8f)
            {
                Press(KombatButtons.HighKick, 0, 0);
            }
            else if (choice < 0.9f)
            {
                Hold(new KombatInputFrame { Held = KombatButtons.Block }, 16);
            }
            else
            {
                Walk(-facing, 12);
            }
        }

        private void DecideFinisher(KombatFighter me, KombatFighter opponent, float distance)
        {
            if (!opponent.CanBeFinished)
            {
                return;
            }

            FighterSpecialMove fatality = me.Match.FatalityFor(me);
            if (Random.value > 0.3f + Difficulty * 0.6f || fatality == null)
            {
                Walk(me.Facing, distance > 1.4f ? 12 : 0);
                Press(KombatButtons.HighPunch, -1, 0);
                return;
            }

            if (distance > KombatMatch.FatalityRange - 0.4f)
            {
                Walk(me.Facing, 8);
                return;
            }

            if (distance < 1.0f)
            {
                Walk(-me.Facing, 6);
                return;
            }

            KombatCommand command = KombatCommand.Parse(fatality.InputCommand);
            if (command != null)
            {
                foreach (KombatInputFrame frame in command.ToFrames(me.Facing)) plan.Enqueue(frame);
                wait = 60;
            }
        }

        private bool TrySpecial(KombatFighter me, AbilityKind kind, float chance)
        {
            if (Random.value > chance)
            {
                return false;
            }

            List<(FighterSpecialMove move, KombatCommand command, AbilityPreset preset)> options = me.SpecialList();
            List<KombatCommand> matches = new List<KombatCommand>();
            foreach ((FighterSpecialMove move, KombatCommand command, AbilityPreset preset) in options)
            {
                if (preset.Kind != kind) continue;
                if (preset.Kind == AbilityKind.Projectile && me.HasActiveProjectile) continue;
                if (preset.Kind == AbilityKind.Ultimate) continue;
                matches.Add(command);
            }

            if (matches.Count == 0)
            {
                return false;
            }

            KombatCommand chosen = matches[Random.Range(0, matches.Count)];
            plan.Enqueue(new KombatInputFrame());
            foreach (KombatInputFrame frame in chosen.ToFrames(me.Facing)) plan.Enqueue(frame);
            return true;
        }

        private void Walk(int direction, int frames)
        {
            if (frames <= 0)
            {
                return;
            }

            Hold(new KombatInputFrame { Horizontal = direction }, frames);
        }

        private void Hold(KombatInputFrame frame, int frames)
        {
            frame.Pressed = frame.Held;
            plan.Enqueue(frame);
            frame.Pressed = KombatButtons.None;
            for (int i = 1; i < frames; i++) plan.Enqueue(frame);
        }

        private void Press(KombatButtons button, int vertical, int horizontal)
        {
            KombatInputFrame setup = new KombatInputFrame { Vertical = vertical, Horizontal = horizontal };
            plan.Enqueue(setup);
            plan.Enqueue(new KombatInputFrame
            {
                Vertical = vertical, Horizontal = horizontal, Held = button, Pressed = button
            });
            plan.Enqueue(setup);
        }

        private void Combo(params KombatButtons[] buttons)
        {
            foreach (KombatButtons button in buttons)
            {
                Press(button, 0, 0);
                for (int i = 0; i < 9; i++) plan.Enqueue(new KombatInputFrame());
            }
        }
    }
}
