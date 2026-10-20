using System.Collections.Generic;
using DevKombat.Fighters;
using UnityEngine;

namespace DevKombat.Kombat
{
    public enum FighterState
    {
        Locked,
        Idle,
        Walk,
        Crouch,
        Jump,
        Block,
        CrouchBlock,
        Attack,
        Special,
        Hitstun,
        Blockstun,
        Knocked,
        Grounded,
        GetUp,
        Held,
        Dizzy,
        Frozen,
        Lifted,
        Victory,
        Defeat,
        Cinematic
    }

    /// <summary>Informações de um golpe que acertou.</summary>
    public struct KombatHit
    {
        public float Damage;
        public HitLevel Level;
        public HitReaction Reaction;
        public float Pushback;
        public int Hitstun;
        public bool Special;
        public Vector2 Point;
        public string ImpactFolder;
        public AudioClip Sound;
    }

    /// <summary>
    /// Lutador estilo Mortal Kombat 2. A lógica roda em quadros fixos (60 por segundo),
    /// chamada pelo <see cref="KombatMatch"/>.
    /// </summary>
    public class KombatFighter : MonoBehaviour
    {
        public const float Gravity = 62f;
        public const float JumpSpeed = 20.5f;
        public const float WalkSpeed = 3.3f;
        public const float BodyHalfWidth = 0.42f;
        public const float StandHeight = 3.4f;
        public const float CrouchHeight = 2.1f;
        public const int MaxJuggles = 4;

        private enum AnimMode { Loop, PingPong, Hold, Progress }

        private class SpecialEntry
        {
            public FighterSpecialMove Move;
            public KombatCommand Command;
            public AbilityPreset Preset;
            public bool Ultimate;
        }

        private class SpecialContext
        {
            public SpecialEntry Entry;
            public int Frame;
            public int HitsLanded;
            public bool Spawned;
            public bool Connected;
        }

        // --- Configuração ---
        public FighterData Data { get; private set; }
        public int PlayerIndex { get; private set; }
        public KombatFighter Opponent { get; set; }
        public KombatMatch Match { get; private set; }
        public IKombatInputSource InputSource { get; set; }
        public Color CostumeColor { get; private set; }
        public string DisplayName => Data != null ? Data.FighterName : "???";

        // --- Estado ---
        public FighterState State { get; private set; }
        public int Facing { get; private set; } = 1;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public float Meter { get; private set; }
        public bool Invisible { get; private set; }
        public bool IsAirborne => Position.y > 0.01f;
        public bool IsDown => State == FighterState.Knocked || State == FighterState.Grounded || State == FighterState.Defeat;
        public bool CanBeFinished => State == FighterState.Dizzy && finishDizzy;
        public bool HasActiveProjectile => activeProjectile != null;
        public int ComboCount { get; private set; }
        public KombatInputBuffer Buffer { get; } = new KombatInputBuffer();
        public KombatInputFrame LastInput { get; private set; }
        public KombatMove CurrentMove => currentMove;
        public bool IsAttacking => State == FighterState.Attack || State == FighterState.Special;

        private readonly List<SpecialEntry> specials = new List<SpecialEntry>();
        private KombatAnimationLibrary library;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer costumeRenderer;
        private SpriteRenderer shadowRenderer;
        private Transform visual;
        private float punchScale = 1f;
        private float kickScale = 1f;
        private float speedScale = 1f;
        private float jumpScale = 1f;

        private int stateFrame;
        private int stateDuration;
        private KombatMove currentMove;
        private int moveFrame;
        private bool moveHit;
        private SpecialContext special;
        private KombatProjectile activeProjectile;
        private int juggleCount;
        private int hitstop;
        private int frozenFrames;
        private int dizzyFrames;
        private bool finishDizzy;
        private KombatFighter holder;
        private Vector2 pullTarget;
        private bool airAttackUsed;
        private int lastHitFrame;
        private int globalFrame;

        private string animKey;
        private AnimMode animMode;
        private float animFps;
        private float animTime;
        private KombatFrames animFrames;
        private float animProgress;
        private int flashFrames;

        public void Setup(FighterData data, int playerIndex, KombatMatch match, IKombatInputSource input, Color? costumeOverride = null)
        {
            Data = data;
            PlayerIndex = playerIndex;
            Match = match;
            InputSource = input;
            CostumeColor = costumeOverride ?? (data != null ? data.CostumeColor : Color.yellow);
            library = new KombatAnimationLibrary(data);

            MaxHealth = data != null ? Mathf.Max(1f, data.MaxHealth) : 100f;
            punchScale = data != null ? Mathf.Max(0.2f, data.PunchDamage / 10f) : 1f;
            kickScale = data != null ? Mathf.Max(0.2f, data.KickDamage / 15f) : 1f;
            speedScale = data != null ? Mathf.Clamp(data.MoveSpeed / 5f, 0.5f, 1.8f) : 1f;
            jumpScale = data != null ? Mathf.Clamp(data.JumpForce / 12f, 0.7f, 1.4f) : 1f;

            BuildRenderers();
            BuildSpecials();
            ResetForRound(playerIndex == 0 ? -2.6f : 2.6f);
        }

        private void BuildRenderers()
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);

            bodyRenderer = new GameObject("Body").AddComponent<SpriteRenderer>();
            bodyRenderer.transform.SetParent(visual, false);
            costumeRenderer = new GameObject("Costume").AddComponent<SpriteRenderer>();
            costumeRenderer.transform.SetParent(visual, false);
            shadowRenderer = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadowRenderer.transform.SetParent(transform, false);
            shadowRenderer.sprite = KombatResources.Shadow;
            shadowRenderer.sortingOrder = -5;
            SetSorting(PlayerIndex == 0 ? 2 : 0);

            float scale = library.HasCustomSprites ? Data.VisualScale : 1f;
            visual.localScale = new Vector3(scale, scale, 1f);
        }

        private void SetSorting(int order)
        {
            bodyRenderer.sortingOrder = order;
            costumeRenderer.sortingOrder = order + 1;
        }

        private void BuildSpecials()
        {
            specials.Clear();
            if (Data == null)
            {
                return;
            }

            foreach (FighterSpecialMove move in Data.SpecialMoves)
            {
                AddSpecial(move, false);
            }

            AddSpecial(Data.UltimateMove, true);
            specials.Sort((a, b) => b.Command.Complexity.CompareTo(a.Command.Complexity));
        }

        private void AddSpecial(FighterSpecialMove move, bool ultimate)
        {
            if (move == null || !move.IsConfigured)
            {
                return;
            }

            AbilityPreset preset = AbilityPreset.Get(move.Ability);
            KombatCommand command = KombatCommand.Parse(move.InputCommand);
            if (preset == null || command == null || !command.IsValid || preset.Kind == AbilityKind.Fatality)
            {
                return;
            }

            specials.Add(new SpecialEntry
            {
                Move = move,
                Command = command,
                Preset = preset,
                Ultimate = ultimate || preset.Kind == AbilityKind.Ultimate
            });
        }

        public void ResetForRound(float x)
        {
            Position = new Vector2(x, 0f);
            Velocity = Vector2.zero;
            Health = MaxHealth;
            Facing = x < 0f ? 1 : -1;
            Invisible = false;
            frozenFrames = 0;
            dizzyFrames = 0;
            finishDizzy = false;
            juggleCount = 0;
            ComboCount = 0;
            holder = null;
            special = null;
            currentMove = null;
            hitstop = 0;
            flashFrames = 0;
            pullFrames = 0;
            hidden = false;
            crouchBlocking = false;
            visual.localRotation = Quaternion.identity;
            float scale = library.HasCustomSprites ? Data.VisualScale : 1f;
            visual.localScale = new Vector3(scale, scale, 1f);
            if (activeProjectile != null)
            {
                activeProjectile.Remove();
                activeProjectile = null;
            }

            Buffer.Clear();
            SetState(FighterState.Locked);
            Play("idle", AnimMode.PingPong, 8f);
            ApplyTransform();
        }

        public void RestoreHealth(float amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        public void AddMeter(float amount)
        {
            Meter = Mathf.Clamp(Meter + amount, 0f, 100f);
        }

        public void SetMeter(float value)
        {
            Meter = Mathf.Clamp(value, 0f, 100f);
        }

        /// <summary>Tira o controle mesmo no meio de golpes, pulos ou especiais.</summary>
        public void ForceLock()
        {
            if (State == FighterState.Victory || State == FighterState.Defeat || State == FighterState.Cinematic ||
                State == FighterState.Knocked || State == FighterState.Grounded || Health <= 0f)
            {
                return;
            }

            special = null;
            currentMove = null;
            holder = null;
            pullFrames = 0;
            Invisible = false;
            bodyRenderer.color = Color.white;
            Velocity.x = 0f;
            if (activeProjectile != null)
            {
                activeProjectile.Remove();
                activeProjectile = null;
            }

            if (IsAirborne)
            {
                SetState(FighterState.Jump);
                airAttackUsed = true;
                Play("jump_up", AnimMode.Hold, 10f);
                return;
            }

            SetState(FighterState.Locked);
            Play("idle", AnimMode.PingPong, 8f);
        }

        public void Unlock()
        {
            if (State == FighterState.Locked)
            {
                SetState(FighterState.Idle);
            }
        }

        public void Lock()
        {
            if (State == FighterState.Idle || State == FighterState.Walk || State == FighterState.Crouch ||
                State == FighterState.Block || State == FighterState.CrouchBlock)
            {
                SetState(FighterState.Locked);
                Play("idle", AnimMode.PingPong, 8f);
            }
        }

        // ------------------------------------------------------------------
        // Quadro a quadro
        // ------------------------------------------------------------------

        public void Tick()
        {
            globalFrame++;
            KombatInputFrame input = InputSource != null && Opponent != null
                ? InputSource.Read(this, Opponent)
                : new KombatInputFrame();
            LastInput = input;

            if (hitstop > 0)
            {
                hitstop--;
                if (input.Pressed != KombatButtons.None || input.Horizontal != 0 || input.Vertical != 0)
                {
                    Buffer.Push(input, Facing);
                }

                return;
            }

            Buffer.Push(input, Facing);
            stateFrame++;

            switch (State)
            {
                case FighterState.Locked:
                    Velocity.x = 0f;
                    break;
                case FighterState.Idle:
                case FighterState.Walk:
                case FighterState.Crouch:
                case FighterState.Block:
                case FighterState.CrouchBlock:
                    TickGroundControl(input);
                    break;
                case FighterState.Jump:
                    TickJump(input);
                    break;
                case FighterState.Attack:
                    TickAttack(input);
                    break;
                case FighterState.Special:
                    TickSpecial();
                    break;
                case FighterState.Hitstun:
                case FighterState.Blockstun:
                    Velocity.x = Mathf.MoveTowards(Velocity.x, 0f, 30f / 60f);
                    if (stateFrame >= stateDuration)
                    {
                        ReturnToNeutral();
                    }

                    break;
                case FighterState.Knocked:
                    if (!IsAirborne && Velocity.y <= 0f && stateFrame > 2)
                    {
                        Land();
                    }

                    break;
                case FighterState.Grounded:
                    Velocity.x = 0f;
                    if (stateFrame >= stateDuration)
                    {
                        if (Health <= 0f)
                        {
                            break;
                        }

                        SetState(FighterState.GetUp, 20);
                        Play("get_up", AnimMode.Progress);
                    }

                    break;
                case FighterState.GetUp:
                    animProgress = stateFrame / (float)stateDuration;
                    if (stateFrame >= stateDuration)
                    {
                        ReturnToNeutral();
                    }

                    break;
                case FighterState.Held:
                    TickHeld();
                    break;
                case FighterState.Dizzy:
                    Velocity.x = 0f;
                    if (!finishDizzy && stateFrame >= dizzyFrames)
                    {
                        ReturnToNeutral();
                    }

                    break;
                case FighterState.Frozen:
                    Velocity.x = 0f;
                    if (stateFrame >= frozenFrames)
                    {
                        bodyRenderer.color = Color.white;
                        ReturnToNeutral();
                    }

                    break;
                case FighterState.Lifted:
                    if (stateFrame < 40)
                    {
                        Velocity = new Vector2(0f, 3.2f);
                    }
                    else
                    {
                        SetState(FighterState.Knocked);
                        Play("knockdown", AnimMode.Hold, 10f);
                    }

                    break;
                case FighterState.Victory:
                case FighterState.Defeat:
                case FighterState.Cinematic:
                    Velocity.x = State == FighterState.Defeat && !IsAirborne ? 0f : Velocity.x;
                    break;
            }

            IntegratePhysics();
            if (flashFrames > 0)
            {
                flashFrames--;
            }
        }

        private void TickGroundControl(KombatInputFrame input)
        {
            FaceOpponent();
            if (Match != null && Match.TryFinisher(this))
            {
                return;
            }

            KombatDirection dir = input.Relative(Facing);
            bool down = (dir & KombatDirection.Down) != 0;
            bool blocking = input.IsHeld(KombatButtons.Block);

            if (TryStartSpecial())
            {
                return;
            }

            if (input.IsPressed(KombatButtons.LowPunch) && (dir & (KombatDirection.Forward | KombatDirection.Back)) != 0 &&
                TryThrow())
            {
                return;
            }

            if (!blocking || input.Pressed != KombatButtons.Block)
            {
                KombatMove normal = PickGroundNormal(input, dir);
                if (normal != null)
                {
                    StartMove(normal);
                    return;
                }
            }

            if (blocking)
            {
                Velocity.x = 0f;
                if (down)
                {
                    if (State != FighterState.CrouchBlock)
                    {
                        SetState(FighterState.CrouchBlock);
                        Play("crouch_block", AnimMode.Hold, 20f);
                    }
                }
                else if (State != FighterState.Block)
                {
                    SetState(FighterState.Block);
                    Play("block", AnimMode.Hold, 20f);
                }

                return;
            }

            if ((dir & KombatDirection.Up) != 0)
            {
                int horizontal = (dir & KombatDirection.Forward) != 0 ? Facing :
                    (dir & KombatDirection.Back) != 0 ? -Facing : 0;
                StartJump(horizontal);
                return;
            }

            if (down)
            {
                Velocity.x = 0f;
                if (State != FighterState.Crouch)
                {
                    SetState(FighterState.Crouch);
                    Play("crouch", AnimMode.Hold, 18f);
                }

                return;
            }

            if (input.Horizontal != 0)
            {
                bool forward = input.Horizontal == Facing;
                Velocity.x = input.Horizontal * WalkSpeed * speedScale * (forward ? 1f : 0.8f);
                if (State != FighterState.Walk)
                {
                    SetState(FighterState.Walk);
                    Play("walk", AnimMode.Loop, 12f);
                }

                animFps = forward ? 12f : -12f;
                return;
            }

            Velocity.x = 0f;
            if (State != FighterState.Idle)
            {
                SetState(FighterState.Idle);
                Play("idle", AnimMode.PingPong, 8f);
            }
        }

        private KombatMove PickGroundNormal(KombatInputFrame input, KombatDirection dir)
        {
            bool down = (dir & KombatDirection.Down) != 0;
            bool back = (dir & KombatDirection.Back) != 0;

            if (down)
            {
                if (input.IsPressed(KombatButtons.HighPunch)) return KombatMoves.Uppercut;
                if (input.IsPressed(KombatButtons.LowPunch)) return KombatMoves.CrouchPunch;
                if (input.IsPressed(KombatButtons.AnyKick)) return KombatMoves.CrouchKick;
                return null;
            }

            if (back)
            {
                if (input.IsPressed(KombatButtons.LowKick)) return KombatMoves.Sweep;
                if (input.IsPressed(KombatButtons.HighKick)) return KombatMoves.Roundhouse;
            }

            if (input.IsPressed(KombatButtons.HighPunch)) return KombatMoves.HighPunch;
            if (input.IsPressed(KombatButtons.LowPunch)) return KombatMoves.LowPunch;
            if (input.IsPressed(KombatButtons.HighKick)) return KombatMoves.HighKick;
            if (input.IsPressed(KombatButtons.LowKick)) return KombatMoves.LowKick;
            return null;
        }

        private void StartJump(int horizontal)
        {
            Velocity = new Vector2(horizontal * 4.6f * speedScale, JumpSpeed * jumpScale);
            Position.y = 0.02f;
            airAttackUsed = false;
            SetState(FighterState.Jump);
            if (horizontal == 0)
            {
                Play("jump_up", AnimMode.Hold, 10f);
            }
            else
            {
                Play("flip", AnimMode.Loop, horizontal == Facing ? 16f : -16f);
            }
        }

        private void TickJump(KombatInputFrame input)
        {
            if (!IsAirborne && Velocity.y <= 0f && stateFrame > 2)
            {
                Velocity.x = 0f;
                SetState(FighterState.Idle);
                Play("idle", AnimMode.PingPong, 8f);
                return;
            }

            if (airAttackUsed)
            {
                return;
            }

            if (input.IsPressed(KombatButtons.AnyPunch))
            {
                StartMove(KombatMoves.JumpPunch);
            }
            else if (input.IsPressed(KombatButtons.AnyKick))
            {
                StartMove(Mathf.Abs(Velocity.x) > 0.5f ? KombatMoves.FlyingKick : KombatMoves.JumpKick);
            }
        }

        private void StartMove(KombatMove move)
        {
            currentMove = move;
            moveFrame = 0;
            moveHit = false;
            if (move.Airborne)
            {
                airAttackUsed = true;
            }
            else
            {
                Velocity.x = 0f;
            }

            SetState(FighterState.Attack);
            Play(move.Animation, move.Airborne ? AnimMode.Hold : AnimMode.Progress, 14f);
            if (move.Sound == "swing_heavy")
            {
                PlayVoice(Data != null ? Data.AttackClips : null, 0.35f);
            }
        }

        private void TickAttack(KombatInputFrame input)
        {
            KombatMove move = currentMove;
            moveFrame++;

            // Janela de cancelamento: um especial digitado logo depois do golpe normal
            // substitui o golpe (facilita comandos com dois botões juntos).
            if (!move.Airborne && moveFrame <= 4 && !moveHit && TryStartSpecial())
            {
                return;
            }

            if (move.Airborne)
            {
                if (!IsAirborne && Velocity.y <= 0f)
                {
                    Velocity.x = 0f;
                    SetState(FighterState.Idle);
                    Play("idle", AnimMode.PingPong, 8f);
                    return;
                }
            }
            else
            {
                animProgress = moveFrame / (float)move.Total;
            }

            if (!moveHit && moveFrame > move.Startup && moveFrame <= move.Startup + move.Active)
            {
                if (move == KombatMoves.Throw)
                {
                    return;
                }

                KombatBox box = HitboxFor(move.Hitbox);
                if (Opponent.CanBeHitBy(box))
                {
                    moveHit = true;
                    float scale = move.IsKick ? kickScale : punchScale;
                    bool landed = Opponent.TakeHit(this, new KombatHit
                    {
                        Damage = move.Damage * scale,
                        Level = move.Level,
                        Reaction = move.Reaction,
                        Pushback = move.Pushback,
                        Hitstun = move.Hitstun,
                        Point = box.Center
                    });
                    if (landed && move == KombatMoves.Uppercut)
                    {
                        Match?.OnUppercutLanded(this);
                    }
                }
            }

            if (!move.Airborne && moveFrame >= move.Total)
            {
                ReturnToNeutral();
            }
            else if (move.Airborne && moveFrame > move.Startup + move.Active + move.Recovery)
            {
                Play(Mathf.Abs(Velocity.x) > 0.5f ? "flip" : "jump_up", AnimMode.Loop, 16f);
            }
        }

        // ------------------------------------------------------------------
        // Arremesso
        // ------------------------------------------------------------------

        private bool TryThrow()
        {
            if (Opponent == null || Opponent.IsAirborne || !Opponent.IsThrowable)
            {
                return false;
            }

            if (Mathf.Abs(Opponent.Position.x - Position.x) > 1.45f)
            {
                return false;
            }

            StartMove(KombatMoves.Throw);
            Opponent.BeThrown(this, KombatMoves.Throw.Damage * punchScale);
            Match?.AddHitstop(6);
            return true;
        }

        public bool IsThrowable =>
            State == FighterState.Idle || State == FighterState.Walk || State == FighterState.Crouch ||
            State == FighterState.Block || State == FighterState.CrouchBlock || State == FighterState.Hitstun ||
            State == FighterState.Attack && currentMove != null && !currentMove.Airborne;

        private void BeThrown(KombatFighter attacker, float damage)
        {
            special = null;
            currentMove = null;
            ApplyDamage(damage, attacker);
            Facing = attacker.Facing;
            Position.y = 0.05f;
            Velocity = new Vector2(-attacker.Facing * 6.5f, 13f);
            juggleCount = MaxJuggles;
            SetState(FighterState.Knocked);
            Play("thrown", AnimMode.Progress);
            animProgress = 0f;
            KombatAudio.RandomSfx("Hits", 0.9f, 0.8f);
            PlayHurtVoice();
        }

        // ------------------------------------------------------------------
        // Especiais
        // ------------------------------------------------------------------

        public IReadOnlyList<string> DescribeSpecials()
        {
            List<string> lines = new List<string>();
            foreach (SpecialEntry entry in specials)
            {
                lines.Add($"{entry.Move.InputCommand}  —  {entry.Move.MoveName}" + (entry.Ultimate ? " (barra cheia)" : ""));
            }

            return lines;
        }

        public List<(FighterSpecialMove move, KombatCommand command, AbilityPreset preset)> SpecialList()
        {
            List<(FighterSpecialMove, KombatCommand, AbilityPreset)> list = new List<(FighterSpecialMove, KombatCommand, AbilityPreset)>();
            foreach (SpecialEntry entry in specials)
            {
                list.Add((entry.Move, entry.Command, entry.Preset));
            }

            return list;
        }

        private bool TryStartSpecial()
        {
            foreach (SpecialEntry entry in specials)
            {
                if (!Buffer.Matches(entry.Command))
                {
                    continue;
                }

                if (entry.Ultimate && Meter < 100f)
                {
                    continue;
                }

                if (entry.Preset.Kind == AbilityKind.Projectile && activeProjectile != null)
                {
                    continue;
                }

                StartSpecial(entry);
                return true;
            }

            return false;
        }

        private void StartSpecial(SpecialEntry entry)
        {
            special = new SpecialContext { Entry = entry };
            currentMove = null;
            Velocity.x = 0f;
            SetState(FighterState.Special);
            AbilityPreset preset = entry.Preset;
            Play(preset.BodyAnimation, AnimMode.Progress);

            if (entry.Ultimate)
            {
                Meter = 0f;
                Match?.OnUltimateStarted(this);
            }

            if (entry.Move.Sound != null)
            {
                KombatAudio.Sfx(entry.Move.Sound);
            }
            else if (!string.IsNullOrEmpty(preset.CastSound))
            {
                KombatAudio.Sfx(preset.CastSound, 0.9f);
            }

            if (!string.IsNullOrWhiteSpace(entry.Move.VoiceLine))
            {
                Match?.ShowLine(this, entry.Move.VoiceLine, 1.4f);
            }
        }

        private float SpecialDamage(SpecialEntry entry)
        {
            float damage = entry.Move.Damage;
            return damage > 0f ? damage : entry.Preset.Damage;
        }

        private void TickSpecial()
        {
            SpecialContext ctx = special;
            if (ctx == null)
            {
                ReturnToNeutral();
                return;
            }

            ctx.Frame++;
            AbilityPreset preset = ctx.Entry.Preset;
            switch (preset.Kind)
            {
                case AbilityKind.Projectile:
                    TickProjectileSpecial(ctx, preset);
                    break;
                case AbilityKind.Teleport:
                    TickTeleport(ctx, preset);
                    break;
                case AbilityKind.Dash:
                    TickDash(ctx, preset);
                    break;
                case AbilityKind.GroundPound:
                    TickGroundPound(ctx, preset);
                    break;
                case AbilityKind.Grab:
                    TickGrab(ctx, preset);
                    break;
                case AbilityKind.Lift:
                case AbilityKind.NutPunch:
                    TickCloseSpecial(ctx, preset);
                    break;
                case AbilityKind.Ultimate:
                    TickUltimate(ctx, preset);
                    break;
                default:
                    ReturnToNeutral();
                    break;
            }
        }

        private void TickProjectileSpecial(SpecialContext ctx, AbilityPreset preset)
        {
            int total = preset.Startup + preset.Recovery;
            animProgress = Mathf.Clamp01(ctx.Frame / (float)(preset.Startup + 6));
            if (!ctx.Spawned && ctx.Frame >= preset.Startup)
            {
                ctx.Spawned = true;
                FighterSpecialMove move = ctx.Entry.Move;
                Sprite[] frames = move.EffectFrames != null && move.EffectFrames.Length > 0
                    ? move.EffectFrames
                    : KombatResources.Frames(preset.EffectFolder);
                activeProjectile = KombatProjectile.Spawn(this, preset, move, frames, SpecialDamage(ctx.Entry));
            }

            if (ctx.Frame >= total && (preset.Reaction != HitReaction.Pull || activeProjectile == null))
            {
                ReturnToNeutral();
            }
        }

        public void OnProjectileFinished(KombatProjectile projectile)
        {
            if (activeProjectile == projectile)
            {
                activeProjectile = null;
            }
        }

        private void TickTeleport(SpecialContext ctx, AbilityPreset preset)
        {
            int vanish = preset.Startup;
            int appear = vanish + 8;
            animProgress = 0f;

            if (ctx.Frame == 1)
            {
                KombatEffect.Play(preset.EffectFolder, Position + Vector2.up * 1.7f, 30f, 1, 0.45f, sortingOrder: 15);
            }

            if (ctx.Frame == vanish)
            {
                Invisible = true;
            }

            if (ctx.Frame == appear)
            {
                float behind = Opponent.Position.x - Opponent.Facing * 1.25f;
                if (preset.Ability == MKAbility.Teleport)
                {
                    behind = Opponent.Position.x + (Position.x < Opponent.Position.x ? 1.4f : -1.4f);
                }

                Position = new Vector2(Match != null ? Match.ClampX(behind) : behind,
                    preset.Ability == MKAbility.TeleportKick ? preset.Height : 0f);
                Velocity = Vector2.zero;
                Invisible = false;
                FaceOpponent(true);
                KombatEffect.Play(preset.EffectFolder, Position + Vector2.up * 1.7f, 30f, 1, 0.45f, sortingOrder: 15);
                Play(preset.BodyAnimation, AnimMode.Progress);
                if (preset.Ability == MKAbility.TeleportKick)
                {
                    Velocity = new Vector2(Facing * 5f, -4f);
                }
            }

            if (ctx.Frame > appear)
            {
                float damage = SpecialDamage(ctx.Entry);
                int attackFrame = ctx.Frame - appear;
                animProgress = Mathf.Clamp01(attackFrame / 18f);
                if (damage > 0f && !ctx.Connected && attackFrame > 3 && attackFrame < 16)
                {
                    KombatBox box = HitboxFor(new Rect(0.2f, preset.Ability == MKAbility.TeleportKick ? 0.3f : 2.2f, 1.3f, 1.2f));
                    if (Opponent.CanBeHitBy(box))
                    {
                        ctx.Connected = true;
                        Opponent.TakeHit(this, MakeSpecialHit(ctx, preset, box.Center));
                    }
                }

                bool landed = preset.Ability != MKAbility.TeleportKick || !IsAirborne;
                if (attackFrame >= (damage > 0f ? 26 : 10) && landed)
                {
                    ReturnToNeutral();
                }
            }
        }

        private void TickDash(SpecialContext ctx, AbilityPreset preset)
        {
            int windup = 6;
            animProgress = preset.BodyAnimation == "flip" ? (ctx.Frame / 4f) % 1f : Mathf.Clamp01(ctx.Frame / 8f);

            if (ctx.Frame < windup)
            {
                Velocity = Vector2.zero;
                return;
            }

            int dashFrame = ctx.Frame - windup;
            if (dashFrame < preset.Duration && !(ctx.Connected && preset.Hits <= 1))
            {
                Velocity = new Vector2(Facing * preset.Speed * speedScale, 0f);
                Position.y = preset.Height;
                if (preset.Afterimage.a > 0f && ctx.Frame % 3 == 0)
                {
                    KombatFX.Afterimage(bodyRenderer, preset.Afterimage);
                }

                int interval = Mathf.Max(1, preset.Duration / Mathf.Max(1, preset.Hits));
                bool canHitAgain = !ctx.Connected || preset.Hits > 1 && dashFrame - lastHitFrame >= interval;
                if (canHitAgain && ctx.HitsLanded < preset.Hits)
                {
                    KombatBox box = HitboxFor(new Rect(-0.1f, preset.Height > 0f ? 0.2f : 0f, 1.5f,
                        preset.Height > 0f ? 1.6f : 0.9f));
                    if (Opponent.CanBeHitBy(box))
                    {
                        ctx.Connected = true;
                        ctx.HitsLanded++;
                        lastHitFrame = dashFrame;
                        KombatHit hit = MakeSpecialHit(ctx, preset, box.Center);
                        if (preset.Hits > 1 && ctx.HitsLanded < preset.Hits)
                        {
                            hit.Reaction = HitReaction.Stagger;
                        }
                        else if (preset.Hits > 1)
                        {
                            hit.Reaction = HitReaction.Knockdown;
                        }

                        Opponent.TakeHit(this, hit);
                    }
                }

                return;
            }

            // Fim da investida: cai e recupera.
            if (Position.y > 0.01f)
            {
                Velocity = new Vector2(Velocity.x * 0.5f, Mathf.Min(Velocity.y, -2f));
                return;
            }

            Velocity.x = ctx.Connected ? -Facing * 2.5f : 0f;
            if (dashFrame >= preset.Duration + preset.Recovery / 2)
            {
                ReturnToNeutral();
            }
        }

        private void TickGroundPound(SpecialContext ctx, AbilityPreset preset)
        {
            animProgress = Mathf.Clamp01(ctx.Frame / (float)preset.Startup);
            if (ctx.Frame == preset.Startup)
            {
                Match?.Shake(0.25f, 0.35f);
                KombatAudio.RandomSfx("Hits", 1f, 0.6f);
                KombatEffect.Play(preset.EffectFolder, new Vector2(Position.x + 1.5f, 0.4f), 16f, 1, 1.2f, sortingOrder: 15);
                KombatEffect.Play(preset.EffectFolder, new Vector2(Position.x - 1.5f, 0.4f), 16f, -1, 1.2f, sortingOrder: 15);
                if (!Opponent.IsAirborne && Mathf.Abs(Opponent.Position.x - Position.x) < 5.5f && Opponent.CanBeHitBy(Opponent.Hurtbox()))
                {
                    Opponent.TakeHit(this, MakeSpecialHit(ctx, preset, Opponent.Position + Vector2.up * 0.5f));
                }
            }

            if (ctx.Frame >= preset.Startup + preset.Recovery)
            {
                ReturnToNeutral();
            }
        }

        private void TickGrab(SpecialContext ctx, AbilityPreset preset)
        {
            if (ctx.Frame == preset.Startup)
            {
                bool close = Mathf.Abs(Opponent.Position.x - Position.x) < 1.6f && !Opponent.IsAirborne && Opponent.IsThrowable;
                if (close)
                {
                    ctx.Connected = true;
                    Opponent.BeHeld(this);
                }
            }

            if (!ctx.Connected)
            {
                animProgress = Mathf.Clamp01(ctx.Frame / (float)preset.Startup);
                if (ctx.Frame >= preset.Startup + preset.Recovery)
                {
                    ReturnToNeutral();
                }

                return;
            }

            int punchFrame = ctx.Frame - preset.Startup;
            int interval = 12;
            int punch = punchFrame / interval;
            if (punch < preset.Hits)
            {
                if (punchFrame % interval == 0)
                {
                    Play(punch % 2 == 0 ? "low_punch" : "high_punch", AnimMode.Progress);
                }

                animProgress = (punchFrame % interval) / (float)interval;
                if (punchFrame % interval == 6)
                {
                    KombatHit hit = MakeSpecialHit(ctx, preset, Opponent.Position + Vector2.up * 2.6f);
                    hit.Reaction = punch == preset.Hits - 1 ? HitReaction.Knockdown : HitReaction.Stagger;
                    Opponent.Release();
                    Opponent.TakeHit(this, hit);
                    if (punch < preset.Hits - 1 && Opponent.Health > 0f)
                    {
                        Opponent.BeHeld(this);
                    }
                }

                return;
            }

            if (punchFrame >= preset.Hits * interval + preset.Recovery)
            {
                ReturnToNeutral();
            }
        }

        private void TickCloseSpecial(SpecialContext ctx, AbilityPreset preset)
        {
            animProgress = Mathf.Clamp01(ctx.Frame / (float)(preset.Startup + 8));
            if (ctx.Frame == preset.Startup)
            {
                if (preset.Kind == AbilityKind.Lift)
                {
                    KombatEffect.Play(preset.EffectFolder, Position + new Vector2(Facing * 1.8f, 1.6f), 24f, Facing, 1.2f,
                        sortingOrder: 15);
                }

                float reach = preset.Kind == AbilityKind.Lift ? 3.5f : 1.3f;
                KombatBox box = HitboxFor(new Rect(0.1f, 0f, reach, 3f));
                if (Opponent.CanBeHitBy(box))
                {
                    Opponent.TakeHit(this, MakeSpecialHit(ctx, preset, box.Center));
                }
            }

            if (ctx.Frame >= preset.Startup + preset.Recovery)
            {
                ReturnToNeutral();
            }
        }

        private void TickUltimate(SpecialContext ctx, AbilityPreset preset)
        {
            animProgress = Mathf.Clamp01(ctx.Frame / (float)preset.Startup);
            if (ctx.Frame == preset.Startup)
            {
                Vector2 target = Opponent.Position + Vector2.up * 1.6f;
                FighterSpecialMove move = ctx.Entry.Move;
                if (move.EffectFrames != null && move.EffectFrames.Length > 0)
                {
                    KombatEffect.Play(move.EffectFrames, target, 18f, Facing, 1.5f, move.EffectTint, sortingOrder: 40);
                }
                else
                {
                    KombatEffect.Play(preset.EffectFolder, target, 18f, Facing, 3.2f, move.EffectTint, sortingOrder: 40);
                }

                Match?.Shake(0.5f, 0.5f);
                KombatAudio.Sfx("Specials/power_hit");
                if (!Opponent.Invisible && !Opponent.IsDown)
                {
                    KombatHit hit = MakeSpecialHit(ctx, preset, target);
                    Opponent.TakeHit(this, hit);
                }
            }

            if (ctx.Frame >= preset.Startup + preset.Recovery)
            {
                ReturnToNeutral();
            }
        }

        private KombatHit MakeSpecialHit(SpecialContext ctx, AbilityPreset preset, Vector2 point)
        {
            return new KombatHit
            {
                Damage = SpecialDamage(ctx.Entry),
                Level = preset.Level,
                Reaction = preset.Reaction,
                Pushback = 1.6f,
                Hitstun = 20,
                Special = true,
                Point = point,
                ImpactFolder = preset.ImpactFolder,
                Sound = ctx.Entry.Move.Sound
            };
        }

        // ------------------------------------------------------------------
        // Receber golpes
        // ------------------------------------------------------------------

        public KombatBox Hurtbox()
        {
            float x = Position.x;
            float y = Position.y;
            switch (State)
            {
                case FighterState.Crouch:
                case FighterState.CrouchBlock:
                    return new KombatBox(x - 0.5f, y, x + 0.5f, y + CrouchHeight);
                case FighterState.Knocked:
                case FighterState.Lifted:
                    return new KombatBox(x - 0.75f, y + 0.2f, x + 0.75f, y + 2.0f);
                case FighterState.Attack when currentMove != null && (currentMove == KombatMoves.CrouchPunch ||
                                                                      currentMove == KombatMoves.CrouchKick ||
                                                                      currentMove == KombatMoves.Sweep):
                    return new KombatBox(x - 0.5f, y, x + 0.5f, y + CrouchHeight);
                case FighterState.Jump:
                    return new KombatBox(x - 0.5f, y + 0.4f, x + 0.5f, y + 2.8f);
                default:
                    return new KombatBox(x - BodyHalfWidth, y, x + BodyHalfWidth, y + StandHeight);
            }
        }

        public bool IsInvulnerable =>
            Invisible || State == FighterState.Grounded || State == FighterState.GetUp ||
            State == FighterState.Defeat || State == FighterState.Victory || State == FighterState.Cinematic ||
            State == FighterState.Locked || State == FighterState.Knocked && juggleCount >= MaxJuggles;

        public bool CanBeHitBy(KombatBox box)
        {
            return !IsInvulnerable && Hurtbox().Overlaps(box);
        }

        public bool IsBlocking => State == FighterState.Block || State == FighterState.CrouchBlock ||
                                  State == FighterState.Blockstun;

        /// <summary>Aplica um golpe. Retorna verdadeiro se acertou (não foi defendido).</summary>
        public bool TakeHit(KombatFighter attacker, KombatHit hit)
        {
            if (IsInvulnerable)
            {
                return false;
            }

            int direction = attacker.Position.x <= Position.x ? 1 : -1;
            bool crouching = State == FighterState.CrouchBlock || State == FighterState.Crouch ||
                             State == FighterState.Blockstun && crouchBlocking;
            bool canBlock = IsBlocking && hit.Level != HitLevel.Unblockable &&
                            !(hit.Level == HitLevel.Overhead && crouching) && !IsAirborne;

            if (canBlock)
            {
                float chip = hit.Special ? hit.Damage * 0.2f : 0f;
                if (chip > 0f)
                {
                    ApplyDamage(chip, attacker);
                }

                Velocity.x = direction * hit.Pushback * 2.4f;
                bool crouchBlock = State == FighterState.CrouchBlock || crouching;
                crouchBlocking = crouchBlock;
                SetState(FighterState.Blockstun, 10);
                Play(crouchBlock ? "crouch_block" : "block", AnimMode.Hold, 20f);
                attacker.Match?.AddHitstop(3);
                KombatAudio.RandomSfx("Hits", 0.45f, 1.5f);
                AddMeter(hit.Damage * 0.2f);
                if (Health <= 0f)
                {
                    // Nocaute pelo dano raspado: cai como qualquer KO.
                    Launch(direction * 4.2f, 9.5f, "knockdown");
                }

                return false;
            }

            if (activeProjectile != null && special != null && special.Entry.Preset.Reaction == HitReaction.Pull)
            {
                activeProjectile.Remove();
            }

            holder = null;
            special = null;
            currentMove = null;
            Invisible = false;
            bodyRenderer.color = Color.white;

            float damage = hit.Damage;
            if (State == FighterState.Knocked || IsAirborne)
            {
                juggleCount++;
                damage *= Mathf.Pow(0.82f, juggleCount);
            }

            ComboCount = globalFrame - lastHitFrameReceived <= 40 ? ComboCount + 1 : 1;
            lastHitFrameReceived = globalFrame;
            ApplyDamage(damage, attacker);
            attacker.AddMeter(damage * 0.6f);
            AddMeter(damage * 0.4f);
            Match?.RegisterHit(attacker, this, damage, ComboCount);

            bool heavy = hit.Reaction == HitReaction.Launch || hit.Reaction == HitReaction.Knockdown ||
                         hit.Reaction == HitReaction.Sweep;
            attacker.Match?.AddHitstop(heavy ? 7 : 4);

            if (!string.IsNullOrEmpty(hit.ImpactFolder))
            {
                KombatEffect.Play(hit.ImpactFolder, hit.Point, 18f, -direction, 1.2f, sortingOrder: 35);
            }

            if (hit.Reaction != HitReaction.Freeze)
            {
                KombatFX.Blood(hit.Point, direction, damage);
            }

            if (hit.Sound != null)
            {
                KombatAudio.Sfx(hit.Sound);
            }
            else
            {
                KombatAudio.RandomSfx("Hits", heavy ? 1f : 0.8f, heavy ? 0.85f : 1f);
            }

            PlayHurtVoice();
            if (Health <= 0f && hit.Reaction != HitReaction.Knockdown && hit.Reaction != HitReaction.Sweep &&
                hit.Reaction != HitReaction.Launch)
            {
                // Quem perdeu a vida sempre cai; nunca fica de pé controlável com 0 de vida.
                hit.Reaction = HitReaction.Knockdown;
            }

            ApplyReaction(hit, direction, attacker);
            return true;
        }

        private int lastHitFrameReceived = -1000;

        private void ApplyReaction(KombatHit hit, int direction, KombatFighter attacker)
        {
            bool airborne = IsAirborne || State == FighterState.Knocked;
            HitReaction reaction = hit.Reaction;
            if (airborne && (reaction == HitReaction.Stagger || reaction == HitReaction.Dizzy))
            {
                reaction = HitReaction.Knockdown;
            }

            switch (reaction)
            {
                case HitReaction.Stagger:
                {
                    bool crouching = State == FighterState.Crouch || State == FighterState.CrouchBlock;
                    Velocity = new Vector2(direction * hit.Pushback * 2.6f, 0f);
                    SetState(FighterState.Hitstun, hit.Hitstun);
                    Play(crouching ? "crouch_hit" : hit.Point.y - Position.y > 2f ? "hit_high" : "hit_low",
                        AnimMode.Hold, 14f);
                    break;
                }
                case HitReaction.Knockdown:
                    Launch(direction * 4.2f, airborne ? 8f : 9.5f, "knockdown");
                    break;
                case HitReaction.Sweep:
                    Launch(direction * 1.5f, 6f, "knockdown");
                    break;
                case HitReaction.Launch:
                    Launch(direction * 2.6f, 19f, "launched");
                    Attacker = attacker;
                    break;
                case HitReaction.Freeze:
                    Velocity = Vector2.zero;
                    frozenFrames = 120;
                    SetState(FighterState.Frozen);
                    bodyRenderer.color = new Color(0.45f, 0.85f, 1f);
                    KombatAudio.Sfx("Specials/ice_hit");
                    break;
                case HitReaction.Pull:
                    pullTarget = new Vector2(attacker.Position.x + attacker.Facing * 1.15f, 0f);
                    Velocity = Vector2.zero;
                    dizzyFrames = 75;
                    finishDizzy = false;
                    SetState(FighterState.Dizzy);
                    Play("dizzy", AnimMode.Loop, 8f);
                    pullFrames = 14;
                    break;
                case HitReaction.Lift:
                    Velocity = new Vector2(0f, 3.2f);
                    Position.y = 0.05f;
                    SetState(FighterState.Lifted);
                    Play("hit_high", AnimMode.Hold, 10f);
                    break;
                case HitReaction.Dizzy:
                    Velocity = Vector2.zero;
                    dizzyFrames = 80;
                    finishDizzy = false;
                    SetState(FighterState.Dizzy);
                    Play("dizzy", AnimMode.Loop, 8f);
                    break;
            }
        }

        public KombatFighter Attacker { get; private set; }

        private int pullFrames;
        private bool crouchBlocking;

        private void Launch(float vx, float vy, string animation)
        {
            Velocity = new Vector2(vx, vy);
            Position.y = Mathf.Max(Position.y, 0.05f);
            SetState(FighterState.Knocked);
            Play(animation, AnimMode.Progress);
            animProgress = 0f;
            knockAnimation = animation;
        }

        private string knockAnimation = "knockdown";

        private void Land()
        {
            Velocity = Vector2.zero;
            Position.y = 0f;
            juggleCount = 0;
            Match?.Shake(0.12f, 0.15f);
            KombatAudio.RandomSfx("Hits", 0.5f, 0.6f);
            Play(knockAnimation == "launched" ? "launched" : "knockdown", AnimMode.Hold, 30f);
            animTime = 99f;
            SetState(FighterState.Grounded, 34);
            Match?.OnFighterLanded(this);
        }

        private void ApplyDamage(float damage, KombatFighter attacker)
        {
            Health = Mathf.Max(0f, Health - damage);
            flashFrames = 4;
            if (Health <= 0f)
            {
                Match?.OnKnockout(this, attacker);
            }
        }

        private void BeHeld(KombatFighter by)
        {
            holder = by;
            special = null;
            currentMove = null;
            Velocity = Vector2.zero;
            SetState(FighterState.Held);
            Play("hit_high", AnimMode.Hold, 10f);
        }

        private void Release()
        {
            holder = null;
        }

        private void TickHeld()
        {
            if (holder == null || holder.State != FighterState.Special)
            {
                ReturnToNeutral();
                return;
            }

            Position = new Vector2(holder.Position.x + holder.Facing * 1.0f, 0f);
            Velocity = Vector2.zero;
        }

        // ------------------------------------------------------------------
        // Controle externo (rounds, FINISH HIM, fatality)
        // ------------------------------------------------------------------

        public void EnterFinishDizzy()
        {
            pullFrames = 0;
            special = null;
            currentMove = null;
            Invisible = false;
            holder = null;
            bodyRenderer.color = Color.white;
            Position.y = 0f;
            Velocity = Vector2.zero;
            finishDizzy = true;
            SetState(FighterState.Dizzy);
            Play("dizzy", AnimMode.Loop, 7f);
        }

        public void SetVictory()
        {
            special = null;
            currentMove = null;
            Invisible = false;
            Velocity.x = 0f;
            FaceOpponent(true);
            SetState(FighterState.Victory);
            Play(Random.value > 0.5f ? "victory" : "victory_dance", AnimMode.Loop, 6f);
            PlayVoice(Data != null ? Data.VictoryClips : null, 1f);
        }

        public void SetDefeat()
        {
            special = null;
            currentMove = null;
            holder = null;
            Invisible = false;
            bodyRenderer.color = Color.white;
            finishDizzy = false;
            bool lying = IsAirborne || State == FighterState.Knocked || State == FighterState.Grounded;
            SetState(FighterState.Defeat);
            if (lying)
            {
                Play(knockAnimation == "launched" ? "launched" : "knockdown", AnimMode.Hold, 10f);
                animTime = 99f;
            }
            else
            {
                Play("defeat", AnimMode.Hold, 9f);
            }
        }

        public void EnterCinematic(string animation = null, float fps = 10f, bool loop = false)
        {
            pullFrames = 0;
            special = null;
            currentMove = null;
            Velocity = Vector2.zero;
            SetState(FighterState.Cinematic);
            if (animation != null)
            {
                Play(animation, loop ? AnimMode.Loop : AnimMode.Hold, fps);
            }
        }

        public void PlayCinematicAnimation(string animation, float fps, bool loop)
        {
            Play(animation, loop ? AnimMode.Loop : AnimMode.Hold, fps);
        }

        public void SetVisible(bool visible)
        {
            hidden = !visible;
        }

        private bool hidden;

        public void SetTint(Color color)
        {
            bodyRenderer.color = color;
            costumeRenderer.color = color * CostumeColor;
        }

        public void SetVisualScale(float scale)
        {
            float baseScale = library.HasCustomSprites ? Data.VisualScale : 1f;
            visual.localScale = new Vector3(baseScale * scale, baseScale * scale, 1f);
        }

        public SpriteRenderer BodyRenderer => bodyRenderer;

        public void ForceFace(int direction)
        {
            Facing = direction >= 0 ? 1 : -1;
        }

        // ------------------------------------------------------------------
        // Física, desenho e utilidades
        // ------------------------------------------------------------------

        private void IntegratePhysics()
        {
            const float dt = 1f / 60f;

            if (pullFrames > 0)
            {
                pullFrames--;
                Position = Vector2.Lerp(Position, pullTarget, 0.35f);
                if (Match != null) Position.x = Match.ClampX(Position.x);
                return;
            }

            bool floating = State == FighterState.Special && special != null &&
                            (special.Entry.Preset.Kind == AbilityKind.Dash && special.Entry.Preset.Height > 0f &&
                             special.Frame >= 6 && special.Frame - 6 < special.Entry.Preset.Duration ||
                             special.Entry.Preset.Kind == AbilityKind.Teleport && Invisible);
            if (State == FighterState.Lifted)
            {
                floating = true;
            }

            if (!floating && (IsAirborne || Velocity.y > 0f))
            {
                Velocity.y -= Gravity * dt;
            }

            Position += Velocity * dt;
            if (Position.y <= 0f)
            {
                Position.y = 0f;
                if (Velocity.y < 0f)
                {
                    Velocity.y = 0f;
                }
            }

            if (Match != null)
            {
                Position.x = Match.ClampX(Position.x);
            }
        }

        /// <summary>Empurra os dois lutadores para que não fiquem um dentro do outro.</summary>
        public static void ResolvePush(KombatFighter a, KombatFighter b)
        {
            if (a.Invisible || b.Invisible || a.IsDown || b.IsDown)
            {
                return;
            }

            if (Mathf.Abs(a.Position.y - b.Position.y) > 2.2f)
            {
                return;
            }

            float minDistance = BodyHalfWidth * 2f;
            float dx = b.Position.x - a.Position.x;
            float overlap = minDistance - Mathf.Abs(dx);
            if (overlap <= 0f)
            {
                return;
            }

            float sign = dx == 0f ? (a.Facing > 0 ? 1f : -1f) : Mathf.Sign(dx);
            a.Position.x -= sign * overlap * 0.5f;
            b.Position.x += sign * overlap * 0.5f;
            if (a.Match != null)
            {
                a.Position.x = a.Match.ClampX(a.Position.x);
                b.Position.x = a.Match.ClampX(b.Position.x);

                // Se um deles estiver preso no canto, o outro recua o que faltou.
                float remaining = minDistance - Mathf.Abs(b.Position.x - a.Position.x);
                if (remaining > 0.001f)
                {
                    float aBefore = a.Position.x;
                    a.Position.x = a.Match.ClampX(a.Position.x - sign * remaining);
                    remaining -= Mathf.Abs(aBefore - a.Position.x);
                    if (remaining > 0.001f)
                    {
                        b.Position.x = a.Match.ClampX(b.Position.x + sign * remaining);
                    }
                }
            }
        }

        private void FaceOpponent(bool force = false)
        {
            if (Opponent == null)
            {
                return;
            }

            if (!force && IsAirborne)
            {
                return;
            }

            float dx = Opponent.Position.x - Position.x;
            if (Mathf.Abs(dx) > 0.05f)
            {
                Facing = dx > 0f ? 1 : -1;
            }
        }

        private KombatBox HitboxFor(Rect local)
        {
            float x0 = Position.x + Facing * local.xMin;
            float x1 = Position.x + Facing * local.xMax;
            return new KombatBox(x0, Position.y + local.yMin, x1, Position.y + local.yMax);
        }

        private void ReturnToNeutral()
        {
            special = null;
            currentMove = null;
            Invisible = false;
            if (Match != null && !Match.FightActive && !Match.FinishActive && !IsAirborne)
            {
                // Fora da luta (fim de round, tempo esgotado) ninguém volta a ter controle.
                Velocity = Vector2.zero;
                SetState(FighterState.Locked);
                Play("idle", AnimMode.PingPong, 8f);
                return;
            }

            if (IsAirborne)
            {
                SetState(FighterState.Jump);
                airAttackUsed = true;
                Play("jump_up", AnimMode.Hold, 10f);
                return;
            }

            Velocity.x = 0f;
            bool crouch = (LastInput.Relative(Facing) & KombatDirection.Down) != 0;
            if (crouch)
            {
                SetState(FighterState.Crouch);
                Play("crouch", AnimMode.Hold, 18f);
                animTime = 99f;
            }
            else
            {
                SetState(FighterState.Idle);
                Play("idle", AnimMode.PingPong, 8f);
            }
        }

        private void SetState(FighterState state, int duration = 0)
        {
            State = state;
            stateFrame = 0;
            stateDuration = duration;
        }

        private void Play(string key, AnimMode mode, float fps = 12f)
        {
            animFrames = library.Get(key);
            animKey = key;
            animMode = mode;
            animFps = fps;
            animTime = 0f;
            animProgress = 0f;
        }

        private void PlayHurtVoice()
        {
            if (Data != null && Data.HitClips.Count > 0)
            {
                PlayVoice(Data.HitClips, 0.8f);
                return;
            }

            if (Random.value < 0.6f)
            {
                KombatAudio.RandomSfx(Data != null && Data.IsFemale ? "VoiceFemale" : "VoiceMale", 0.7f);
            }
        }

        private static void PlayVoice(IReadOnlyList<AudioClip> clips, float chance)
        {
            if (clips == null || clips.Count == 0 || Random.value > chance)
            {
                return;
            }

            KombatAudio.Sfx(clips[Random.Range(0, clips.Count)]);
        }

        private void LateUpdate()
        {
            if (bodyRenderer == null)
            {
                return;
            }

            UpdateAnimation(Time.deltaTime);
            ApplyTransform();
        }

        private void UpdateAnimation(float dt)
        {
            if (animFrames == null || animFrames.Count == 0)
            {
                bodyRenderer.sprite = KombatResources.PlaceholderFighter;
                bodyRenderer.color = State == FighterState.Frozen ? new Color(0.45f, 0.85f, 1f) :
                    flashFrames > 0 ? Color.white : CostumeColor;
                bodyRenderer.transform.localPosition = Vector3.zero;
                bodyRenderer.enabled = !Invisible && !hidden;
                costumeRenderer.sprite = null;
                return;
            }

            bool paused = State == FighterState.Frozen || hitstop > 0;
            if (!paused)
            {
                animTime += dt * animFps;
            }

            int count = animFrames.Count;
            int index;
            switch (animMode)
            {
                case AnimMode.Loop:
                    index = ((Mathf.FloorToInt(animTime) % count) + count) % count;
                    break;
                case AnimMode.PingPong:
                    int cycle = Mathf.Max(1, count * 2 - 2);
                    int step = ((Mathf.FloorToInt(animTime) % cycle) + cycle) % cycle;
                    index = step < count ? step : cycle - step;
                    break;
                case AnimMode.Progress:
                    index = Mathf.Clamp(Mathf.FloorToInt(animProgress * count), 0, count - 1);
                    break;
                default:
                    index = Mathf.Clamp(Mathf.FloorToInt(animTime), 0, count - 1);
                    break;
            }

            Sprite sprite = animFrames.Body[index];
            bodyRenderer.sprite = sprite;

            // Sprites próprios podem ter o pivô no centro: sobe a imagem até os pés tocarem o chão.
            float lift = sprite != null ? -sprite.bounds.min.y : 0f;
            bodyRenderer.transform.localPosition = new Vector3(0f, lift, 0f);
            costumeRenderer.transform.localPosition = bodyRenderer.transform.localPosition;
            bool hasCostume = !animFrames.Custom && animFrames.Costume != null && index < animFrames.Costume.Length;
            costumeRenderer.sprite = hasCostume ? animFrames.Costume[index] : null;

            if (State != FighterState.Frozen && State != FighterState.Cinematic)
            {
                Color tint = flashFrames > 0 ? new Color(1f, 0.6f, 0.6f) : Color.white;
                bodyRenderer.color = tint;
                costumeRenderer.color = CostumeColor * tint;
            }
            else if (State == FighterState.Frozen)
            {
                costumeRenderer.color = new Color(0.45f, 0.85f, 1f);
            }

            bool visible = !Invisible && !hidden;
            bodyRenderer.enabled = visible;
            costumeRenderer.enabled = visible && hasCostume;
        }

        private void ApplyTransform()
        {
            transform.position = new Vector3(Position.x, Position.y, 0f);
            Vector3 scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * Facing;
            visual.localScale = scale;

            shadowRenderer.transform.position = new Vector3(Position.x, 0.02f, 0f);
            float shadowScale = Mathf.Lerp(1.6f, 0.8f, Mathf.Clamp01(Position.y / 4f));
            shadowRenderer.transform.localScale = new Vector3(shadowScale, shadowScale, 1f);
            shadowRenderer.enabled = !Invisible && !hidden;
        }

        public void SetSortingOnTop(bool onTop)
        {
            SetSorting(onTop ? 2 : 0);
        }
    }
}
