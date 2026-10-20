using System;
using System.Collections;
using System.Collections.Generic;
using DevKombat.Fighters;
using UnityEngine;

namespace DevKombat.Kombat
{
    public enum KombatMode
    {
        VersusCpu,
        VersusPlayer,
        Training
    }

    public enum KombatBanner
    {
        None,
        Round,
        Fight,
        FinishHim,
        FinishHer,
        Fatality,
        Babality,
        Wins,
        FlawlessVictory,
        TimeIsUp,
        UltimateFlash
    }

    /// <summary>
    /// Uma partida melhor de três rounds: apresentação, FIGHT!, tempo de 99 segundos,
    /// FINISH HIM/HER com Fatality ou Babality e tela de vencedor.
    /// </summary>
    public class KombatMatch : MonoBehaviour
    {
        public const int RoundsToWin = 2;
        public const int RoundSeconds = 99;
        public const float FatalityRange = 3.4f;
        public const int FinishSeconds = 7;

        public const string BabalityCommand = "Baixo, Baixo, Baixo + Chute Fraco";
        public const string DefaultFatalityCommand = "Frente, Frente, Baixo + Soco Forte";

        public KombatFighter[] Fighters { get; } = new KombatFighter[2];
        public int[] Wins { get; } = new int[2];
        public int Round { get; private set; }
        public float TimeLeft { get; private set; } = RoundSeconds;
        public KombatMode Mode { get; private set; }
        public KombatStage Stage { get; private set; }
        public KombatCamera Camera { get; private set; }
        public bool FightActive { get; private set; }
        public bool FinishActive { get; private set; }
        public bool Paused { get; set; }
        public bool MatchOver { get; private set; }
        public int MatchWinner { get; private set; } = -1;
        public KombatBanner Banner { get; private set; }
        public string BannerText { get; private set; }
        public float BannerTime { get; private set; }
        public int LastComboCount { get; private set; }
        public int LastComboPlayer { get; private set; }
        public float ComboTime { get; private set; }
        public string[] Lines { get; } = new string[2];
        public float[] LineTimes { get; } = new float[2];
        public float ToastyTime { get; private set; }
        public float FlashTime { get; private set; }
        public Color FlashColor { get; private set; }

        public event Action<int> OnMatchFinished;

        private readonly List<KombatProjectile> projectiles = new List<KombatProjectile>();
        private float accumulator;
        private int hitstop;
        private KombatFighter knockedOut;
        private KombatFighter roundWinner;
        private bool roundEnding;
        private bool fatalityStarted;
        private bool hadFatality;
        private bool endedByTimeout;
        private DummyKombatInput trainingDummy;
        private float trainingIdle;

        public static KombatMatch Create(
            FighterData playerOne,
            FighterData playerTwo,
            IKombatInputSource inputOne,
            IKombatInputSource inputTwo,
            KombatStageInfo stageInfo,
            KombatMode mode)
        {
            GameObject go = new GameObject("KombatMatch");
            KombatMatch match = go.AddComponent<KombatMatch>();
            match.Mode = mode;
            match.Camera = KombatCamera.Create();
            match.Stage = KombatStage.Create(stageInfo, match.Camera.Camera.aspect);
            match.Stage.transform.SetParent(go.transform, false);

            Color? mirror = null;
            if (playerOne == playerTwo && playerOne != null)
            {
                // Mesmo lutador dos dois lados: a cor muda, como no MK.
                Color.RGBToHSV(playerOne.CostumeColor, out float h, out float s, out float v);
                mirror = Color.HSVToRGB((h + 0.45f) % 1f, Mathf.Max(0.5f, s), Mathf.Max(0.6f, v));
            }

            match.Fighters[0] = CreateFighter(match, playerOne, 0, inputOne, null);
            match.Fighters[1] = CreateFighter(match, playerTwo, 1, inputTwo, mirror);
            match.Fighters[0].Opponent = match.Fighters[1];
            match.Fighters[1].Opponent = match.Fighters[0];
            match.trainingDummy = inputTwo as DummyKombatInput;

            match.StartCoroutine(match.MatchFlow());
            return match;
        }

        private static KombatFighter CreateFighter(KombatMatch match, FighterData data, int index, IKombatInputSource input, Color? costume)
        {
            GameObject go = new GameObject("Fighter_P" + (index + 1) + "_" + (data != null ? data.FighterName : "?"));
            go.transform.SetParent(match.transform, false);
            KombatFighter fighter = go.AddComponent<KombatFighter>();
            fighter.Setup(data, index, match, input, costume);
            return fighter;
        }

        // ------------------------------------------------------------------
        // Laço principal (60 quadros por segundo)
        // ------------------------------------------------------------------

        private void Update()
        {
            if (Paused)
            {
                return;
            }

            float dt = Time.deltaTime;
            BannerTime = Mathf.Max(0f, BannerTime - dt);
            ComboTime = Mathf.Max(0f, ComboTime - dt);
            ToastyTime = Mathf.Max(0f, ToastyTime - dt);
            FlashTime = Mathf.Max(0f, FlashTime - dt);
            for (int i = 0; i < 2; i++) LineTimes[i] = Mathf.Max(0f, LineTimes[i] - dt);

            for (int i = 0; i < 2; i++)
            {
                (Fighters[i].InputSource as KeyboardKombatInput)?.Poll();
            }

            accumulator = Mathf.Min(accumulator + dt, 0.1f);
            while (accumulator >= 1f / 60f)
            {
                accumulator -= 1f / 60f;
                FixedTick();
            }

            if (Camera != null)
            {
                Camera.Follow((Fighters[0].Position.x + Fighters[1].Position.x) * 0.5f, Stage.HalfWidth);
            }
        }

        private void FixedTick()
        {
            if (hitstop > 0)
            {
                hitstop--;
                return;
            }

            Fighters[0].Tick();
            Fighters[1].Tick();
            KombatFighter.ResolvePush(Fighters[0], Fighters[1]);

            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                if (projectiles[i] == null || !projectiles[i].IsAlive)
                {
                    projectiles.RemoveAt(i);
                }
            }

            CheckProjectileClashes();
            foreach (KombatProjectile projectile in projectiles.ToArray())
            {
                projectile.Tick();
            }

            // Quem ataca aparece na frente.
            bool firstOnTop = Fighters[0].IsAttacking || !Fighters[1].IsAttacking && Fighters[0].PlayerIndex == 0;
            Fighters[0].SetSortingOnTop(firstOnTop);
            Fighters[1].SetSortingOnTop(!firstOnTop);

            if (FightActive && Mode != KombatMode.Training)
            {
                TimeLeft -= 1f / 60f;
                if (TimeLeft <= 0f)
                {
                    TimeLeft = 0f;
                    StartCoroutine(TimeOver());
                }
            }

            if (Mode == KombatMode.Training)
            {
                TickTraining();
            }
        }

        private void CheckProjectileClashes()
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                for (int j = i + 1; j < projectiles.Count; j++)
                {
                    KombatProjectile a = projectiles[i];
                    KombatProjectile b = projectiles[j];
                    if (a.IsAlive && b.IsAlive && a.Owner != b.Owner && a.Box.Overlaps(b.Box))
                    {
                        a.Clash();
                        b.Clash();
                    }
                }
            }
        }

        private void TickTraining()
        {
            KombatFighter dummy = Fighters[1];
            if (dummy.State == FighterState.Idle || dummy.State == FighterState.Locked)
            {
                trainingIdle += 1f / 60f;
                if (trainingIdle > 1.2f)
                {
                    dummy.RestoreHealth(dummy.MaxHealth);
                }
            }
            else
            {
                trainingIdle = 0f;
            }

            Fighters[0].SetMeter(100f);
        }

        // ------------------------------------------------------------------
        // Fluxo de rounds
        // ------------------------------------------------------------------

        private IEnumerator MatchFlow()
        {
            KombatAudio.RandomStageMusic();
            ShowIntroLines();

            while (!MatchOver)
            {
                Round++;
                yield return RoundIntro();
                FightActive = true;
                Fighters[0].Unlock();
                Fighters[1].Unlock();

                while (!roundEnding)
                {
                    yield return null;
                }

                while (roundEnding)
                {
                    yield return null;
                }
            }
        }

        private IEnumerator RoundIntro()
        {
            foreach (KombatProjectile projectile in projectiles.ToArray())
            {
                projectile?.Remove();
            }

            projectiles.Clear();
            Fighters[0].ResetForRound(-2.6f);
            Fighters[1].ResetForRound(2.6f);
            Camera.Snap(0f);
            TimeLeft = RoundSeconds;
            knockedOut = null;
            roundWinner = null;
            fatalityStarted = false;
            FinishActive = false;

            if (Mode == KombatMode.Training)
            {
                SetBanner(KombatBanner.Fight, 1f, "TREINO");
                yield return new WaitForSeconds(0.8f);
                yield break;
            }

            SetBanner(KombatBanner.Round, 1.6f, "ROUND " + Round);
            KombatAudio.Announce("Announcer/round" + Mathf.Clamp(Round, 1, 4));
            yield return new WaitForSeconds(1.6f);
            SetBanner(KombatBanner.Fight, 1f, "FIGHT!");
            KombatAudio.Announce("Announcer/fight");
            yield return new WaitForSeconds(0.5f);
        }

        private void ShowIntroLines()
        {
            for (int i = 0; i < 2; i++)
            {
                FighterData data = Fighters[i].Data;
                if (data != null && data.DialogueLines.Count > 0)
                {
                    ShowLine(Fighters[i], data.DialogueLines[UnityEngine.Random.Range(0, data.DialogueLines.Count)], 3f);
                }
            }

            FighterData first = Fighters[0].Data;
            if (first != null && first.IntroClips.Count > 0)
            {
                KombatAudio.Sfx(first.IntroClips[UnityEngine.Random.Range(0, first.IntroClips.Count)]);
            }
        }

        public void OnKnockout(KombatFighter loser, KombatFighter attacker)
        {
            if (Mode == KombatMode.Training)
            {
                loser.RestoreHealth(loser.MaxHealth);
                return;
            }

            if (!FightActive || knockedOut != null)
            {
                return;
            }

            if (FinishActive)
            {
                return;
            }

            knockedOut = loser;
            roundWinner = loser.Opponent;
            FightActive = false;
            endedByTimeout = false;
            StartCoroutine(EndRound());
        }

        private IEnumerator EndRound()
        {
            roundEnding = true;
            if (roundWinner.Health <= 0f)
            {
                // Os dois caíram ao mesmo tempo: empate, ninguém ganha o round.
                yield return new WaitForSeconds(1.2f);
                Fighters[0].ForceLock();
                Fighters[1].ForceLock();
                SetBanner(KombatBanner.Wins, 2f, "EMPATE");
                yield return new WaitForSeconds(2f);
                roundEnding = false;
                yield break;
            }

            int winnerIndex = roundWinner.PlayerIndex;
            bool matchPoint = Wins[winnerIndex] + 1 >= RoundsToWin && !endedByTimeout;
            bool flawless = roundWinner.Health >= roundWinner.MaxHealth - 0.01f;
            yield return new WaitForSeconds(0.3f);

            if (matchPoint)
            {
                yield return FinishHimPhase();
            }
            else
            {
                while (knockedOut.State == FighterState.Knocked || knockedOut.IsAirborne)
                {
                    yield return null;
                }

                roundWinner.Lock();
                knockedOut.SetDefeat();
                yield return new WaitForSeconds(0.6f);
            }

            Wins[winnerIndex]++;
            roundWinner.SetVictory();
            KombatAudio.StopMusic();
            if (flawless)
            {
                SetBanner(KombatBanner.FlawlessVictory, 2.5f, "FLAWLESS VICTORY");
                KombatAudio.Announce("Announcer/flawless_victory");
                yield return new WaitForSeconds(1.8f);
            }

            ShowWinner(roundWinner);
            yield return new WaitForSeconds(2.8f);

            if (Wins[winnerIndex] >= RoundsToWin)
            {
                MatchOver = true;
                MatchWinner = winnerIndex;
                FighterData data = roundWinner.Data;
                if (data != null && data.DialogueLines.Count > 0)
                {
                    ShowLine(roundWinner, data.DialogueLines[data.DialogueLines.Count - 1], 4f);
                }

                yield return new WaitForSeconds(2.5f);
                OnMatchFinished?.Invoke(winnerIndex);
            }
            else
            {
                KombatAudio.RandomStageMusic();
            }

            roundEnding = false;
        }

        private void ShowWinner(KombatFighter winner)
        {
            SetBanner(KombatBanner.Wins, 3f, winner.DisplayName.ToUpperInvariant() + " VENCE");
            if (winner.Data != null && winner.Data.AnnouncerName != null)
            {
                KombatAudio.Announce(winner.Data.AnnouncerName);
            }
            else if (!hadFatality)
            {
                string[] praise = { "Announcer/excellent", "Announcer/outstanding", "Announcer/superb", "Announcer/well_done" };
                KombatAudio.Announce(praise[UnityEngine.Random.Range(0, praise.Length)]);
            }

            hadFatality = false;
        }

        private IEnumerator FinishHimPhase()
        {
            while (knockedOut.State == FighterState.Knocked || knockedOut.IsAirborne)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);
            projectiles.ForEach(p => p?.Remove());
            knockedOut.EnterFinishDizzy();
            bool female = knockedOut.Data != null && knockedOut.Data.IsFemale;
            SetBanner(female ? KombatBanner.FinishHer : KombatBanner.FinishHim, 2.2f, female ? "FINISH HER!" : "FINISH HIM!");
            KombatAudio.Announce(female ? "Announcer/finish_her" : "Announcer/finish_him");
            if (female && KombatResources.Clip("Announcer/finish_her") == null)
            {
                KombatAudio.Announce("Announcer/finish_him");
            }

            FinishActive = true;
            float timer = FinishSeconds;
            KombatFighter winner = roundWinner;
            if (winner.State == FighterState.Locked)
            {
                winner.Unlock();
            }

            while (timer > 0f && !fatalityStarted)
            {
                if (!Paused)
                {
                    timer -= Time.deltaTime;
                }

                // Qualquer golpe comum derruba quem estava tonto.
                if (!fatalityStarted && knockedOut.State != FighterState.Dizzy)
                {
                    break;
                }

                yield return null;
            }

            FinishActive = false;
            if (fatalityStarted)
            {
                while (fatalityStarted)
                {
                    yield return null;
                }

                yield break;
            }

            winner.Lock();
            while (knockedOut.State == FighterState.Knocked || knockedOut.IsAirborne)
            {
                yield return null;
            }

            knockedOut.SetDefeat();
            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Chamado pelo lutador antes de ler golpes normais: confere se o comando de
        /// Fatality ou Babality foi feito durante o FINISH HIM/HER.
        /// </summary>
        public bool TryFinisher(KombatFighter winner)
        {
            if (!FinishActive || fatalityStarted || winner != roundWinner || knockedOut == null ||
                !knockedOut.CanBeFinished)
            {
                return false;
            }

            float distance = Mathf.Abs(winner.Position.x - knockedOut.Position.x);
            FighterSpecialMove fatality = FatalityFor(winner);
            KombatCommand fatalityCommand = KombatCommand.Parse(fatality.InputCommand);
            KombatCommand babalityCommand = KombatCommand.Parse(BabalityCommand);

            if (distance <= FatalityRange && winner.Buffer.Matches(fatalityCommand))
            {
                StartCoroutine(PerformFinisher(winner, knockedOut, fatality.Ability, fatality));
                return true;
            }

            if (distance <= FatalityRange + 1f && winner.Buffer.Matches(babalityCommand))
            {
                StartCoroutine(PerformFinisher(winner, knockedOut, MKAbility.Babality, null));
                return true;
            }

            return false;
        }

        /// <summary>Fatality do lutador ou a padrão (explosão) quando ele não tiver uma configurada.</summary>
        public FighterSpecialMove FatalityFor(KombatFighter fighter)
        {
            FighterSpecialMove fatality = fighter.Data != null ? fighter.Data.Fatality : null;
            if (fatality != null && fatality.IsConfigured)
            {
                return fatality;
            }

            return new FighterSpecialMove("Fatality", DefaultFatalityCommand, MKAbility.FatalityExplode, 0f);
        }

        private IEnumerator PerformFinisher(KombatFighter winner, KombatFighter victim, MKAbility ability, FighterSpecialMove move)
        {
            fatalityStarted = true;
            hadFatality = true;
            KombatAudio.StopMusic();
            winner.ForceFace(victim.Position.x >= winner.Position.x ? 1 : -1);
            victim.ForceFace(-winner.Facing);
            winner.EnterCinematic();
            victim.EnterCinematic("dizzy", 7f, true);
            if (move != null && !string.IsNullOrWhiteSpace(move.VoiceLine))
            {
                ShowLine(winner, move.VoiceLine, 2.5f);
            }

            Shake(0.1f, 0.05f);
            FlashColor = new Color(0f, 0f, 0f, 0.55f);
            FlashTime = 0.6f;
            yield return new WaitForSeconds(0.6f);

            switch (ability)
            {
                case MKAbility.FatalityFire:
                    yield return FatalityFire(winner, victim, move);
                    break;
                case MKAbility.FatalityFreeze:
                    yield return FatalityFreeze(winner, victim, move);
                    break;
                case MKAbility.FatalityUppercut:
                    yield return FatalityUppercut(winner, victim);
                    break;
                case MKAbility.Babality:
                    yield return Babality(winner, victim);
                    break;
                default:
                    yield return FatalityExplode(winner, victim, move);
                    break;
            }

            if (ability != MKAbility.Babality)
            {
                SetBanner(KombatBanner.Fatality, 3f, "FATALITY");
                KombatAudio.Announce(KombatResources.Clip("Announcer/fatality") != null ? "Announcer/fatality" : "Announcer/laugh");
                KombatAudio.Sfx("Announcer/gong");
            }

            yield return new WaitForSeconds(2.4f);
            fatalityStarted = false;
        }

        private IEnumerator FatalityFire(KombatFighter winner, KombatFighter victim, FighterSpecialMove move)
        {
            winner.PlayCinematicAnimation("fatality_unmask", 6f, false);
            yield return new WaitForSeconds(1f);
            KombatAudio.Sfx("Specials/power_hit");
            Vector2 start = winner.Position + new Vector2(winner.Facing * 0.7f, 3f);
            Vector2 end = victim.Position + Vector2.up * 1.8f;
            Sprite[] fire = Custom(move, "FX/fire_ball");
            KombatEffect ball = KombatEffect.Play(fire, start, 12f, winner.Facing, 1.6f, Tint(move), true, 2f, 40);
            float t = 0f;
            while (t < 1f && ball != null)
            {
                t += Time.deltaTime * 2.2f;
                ball.transform.position = Vector2.Lerp(start, end, t);
                yield return null;
            }

            if (ball != null) Destroy(ball.gameObject);
            KombatEffect.Play("FX/fire_explosion", victim.Position + Vector2.up * 1.2f, 12f, 1, 1.4f, sortingOrder: 40);
            victim.SetVisible(false);
            KombatEffect burning = KombatEffect.Play("FX/burning_body", victim.Position, 7f, victim.Facing, 1f,
                lifetime: 1.8f, sortingOrder: 30);
            if (burning != null)
            {
                burning.Renderer.sprite = burning.Renderer.sprite;
                Vector3 p = burning.transform.position;
                burning.transform.position = new Vector3(p.x, p.y + 1.85f, p.z);
            }

            KombatAudio.Sfx("Specials/crispy");
            KombatAudio.Sfx(victim.Data != null && victim.Data.IsFemale ? "Specials/female_brutality" : "Specials/male_brutality");
            yield return new WaitForSeconds(1.7f);
            KombatEffect.Play("FX/hell_fire", victim.Position + Vector2.up * 1.2f, 14f, 1, 1.2f, sortingOrder: 31);
            KombatFX.Gibs(victim.Position, 10, 6f);
            winner.SetVictory();
        }

        private IEnumerator FatalityFreeze(KombatFighter winner, KombatFighter victim, FighterSpecialMove move)
        {
            winner.PlayCinematicAnimation("special_lunge", 10f, false);
            KombatAudio.Sfx("Specials/ice_cast");
            Sprite[] ice = Custom(move, "FX/ice_blast");
            Vector2 start = winner.Position + new Vector2(winner.Facing * 0.9f, 2.4f);
            KombatEffect blast = KombatEffect.Play(ice, start, 14f, winner.Facing, 1.2f, Tint(move), true, 2f, 40);
            float t = 0f;
            while (t < 1f && blast != null)
            {
                t += Time.deltaTime * 2.5f;
                blast.transform.position = Vector2.Lerp(start, victim.Position + Vector2.up * 2f, t);
                yield return null;
            }

            if (blast != null) Destroy(blast.gameObject);
            KombatAudio.Sfx("Specials/ice_hit");
            victim.SetTint(new Color(0.45f, 0.85f, 1f));
            KombatEffect.Play("FX/ice_ground", victim.Position + Vector2.up * 0.1f, 10f, 1, 1.2f, lifetime: 2.5f, sortingOrder: 29);
            yield return new WaitForSeconds(1.2f);
            winner.PlayCinematicAnimation("uppercut", 14f, false);
            yield return new WaitForSeconds(0.35f);
            Shake(0.4f, 0.4f);
            victim.SetVisible(false);
            KombatFX.IceShards(victim.Position, 22);
            KombatAudio.RandomSfx("Hits", 1f, 0.7f);
            yield return new WaitForSeconds(1f);
            winner.SetVictory();
        }

        private IEnumerator FatalityExplode(KombatFighter winner, KombatFighter victim, FighterSpecialMove move)
        {
            winner.PlayCinematicAnimation("high_punch", 8f, false);
            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                float pulse = 1f + Mathf.Sin(t * 18f) * 0.04f * t;
                victim.SetVisualScale(pulse);
                victim.SetTint(Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), t / 1.4f));
                yield return null;
            }

            Shake(0.6f, 0.6f);
            Sprite[] boom = Custom(move, "FX/explosion");
            KombatEffect.Play(boom, victim.Position + Vector2.up * 1.8f, 16f, 1, 1.4f, Tint(move), sortingOrder: 41);
            victim.SetVisible(false);
            KombatFX.Gibs(victim.Position, 26, 11f);
            KombatAudio.Sfx(victim.Data != null && victim.Data.IsFemale ? "Specials/female_brutality" : "Specials/male_brutality");
            KombatAudio.RandomSfx("Hits", 1f, 0.6f);
            yield return new WaitForSeconds(1f);
            winner.SetVictory();
        }

        private IEnumerator FatalityUppercut(KombatFighter winner, KombatFighter victim)
        {
            winner.PlayCinematicAnimation("uppercut", 12f, false);
            yield return new WaitForSeconds(0.3f);
            Shake(0.5f, 0.5f);
            KombatAudio.RandomSfx("Hits", 1f, 0.7f);
            KombatAudio.Sfx(victim.Data != null && victim.Data.IsFemale ? "Specials/female_brutality" : "Specials/male_brutality");
            victim.PlayCinematicAnimation("launched", 8f, false);
            float t = 0f;
            Vector2 start = victim.Position;
            while (t < 1.6f)
            {
                t += Time.deltaTime;
                victim.Position = start + new Vector2(-winner.Facing * t * 1.5f, Mathf.Sin(Mathf.Min(t / 1.6f, 1f) * Mathf.PI) * 6f);
                if (UnityEngine.Random.value < 0.4f)
                {
                    KombatFX.Blood(victim.Position + Vector2.up * 1.5f, -winner.Facing, 6f);
                }

                yield return null;
            }

            victim.Position = new Vector2(victim.Position.x, 0f);
            KombatEffect.Play("FX/blood_fountain", victim.Position + Vector2.up * 0.6f, 12f, 1, 1.6f, sortingOrder: 31);
            KombatEffect.Play("FX/blood_pool", victim.Position + Vector2.up * 0.05f, 6f, 1, 1.4f, lifetime: 4f, sortingOrder: -1);
            victim.PlayCinematicAnimation("knockdown", 30f, false);
            winner.SetVictory();
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator Babality(KombatFighter winner, KombatFighter victim)
        {
            winner.PlayCinematicAnimation("victory_dance", 6f, true);
            yield return new WaitForSeconds(0.8f);
            KombatEffect.Play("FX/portal", victim.Position + Vector2.up * 1.6f, 30f, 1, 0.45f, sortingOrder: 40);
            yield return new WaitForSeconds(0.3f);
            bool hasBaby = KombatResources.Frames("Fighters/Ninja/baby").Length > 0;
            victim.PlayCinematicAnimation(hasBaby ? "baby" : "idle", 6f, true);
            victim.SetVisualScale(hasBaby ? 1f : 0.35f);
            SetBanner(KombatBanner.Babality, 3f, "BABALITY");
            KombatAudio.Announce("Announcer/laugh2");
            yield return new WaitForSeconds(1f);
            winner.SetVictory();
        }

        private static Sprite[] Custom(FighterSpecialMove move, string fallback)
        {
            return move != null && move.EffectFrames != null && move.EffectFrames.Length > 0
                ? move.EffectFrames
                : KombatResources.Frames(fallback);
        }

        private static Color? Tint(FighterSpecialMove move)
        {
            return move != null ? move.EffectTint : (Color?)null;
        }

        private IEnumerator TimeOver()
        {
            if (!FightActive)
            {
                yield break;
            }

            FightActive = false;
            roundEnding = true;
            Fighters[0].ForceLock();
            Fighters[1].ForceLock();
            SetBanner(KombatBanner.TimeIsUp, 2f, "TIME IS UP");
            yield return new WaitForSeconds(2f);

            if (Mathf.Approximately(Fighters[0].Health, Fighters[1].Health))
            {
                SetBanner(KombatBanner.Wins, 2f, "EMPATE");
                yield return new WaitForSeconds(2f);
                roundEnding = false;
                yield break;
            }

            roundWinner = Fighters[0].Health > Fighters[1].Health ? Fighters[0] : Fighters[1];
            knockedOut = roundWinner.Opponent;
            endedByTimeout = true;
            roundEnding = false;
            StartCoroutine(EndRound());
            yield return null;
        }

        // ------------------------------------------------------------------
        // Serviços para os lutadores
        // ------------------------------------------------------------------

        public void RegisterProjectile(KombatProjectile projectile)
        {
            projectiles.Add(projectile);
        }

        public float ProjectileThreat(KombatFighter target)
        {
            float closest = float.MaxValue;
            foreach (KombatProjectile projectile in projectiles)
            {
                if (projectile == null || !projectile.IsAlive || projectile.Owner == target)
                {
                    continue;
                }

                float dx = target.Position.x - projectile.Position.x;
                if (Mathf.Sign(dx) == projectile.Direction)
                {
                    closest = Mathf.Min(closest, Mathf.Abs(dx));
                }
            }

            return closest;
        }

        public void AddHitstop(int frames)
        {
            hitstop = Mathf.Max(hitstop, frames);
        }

        public void Shake(float seconds, float strength)
        {
            Camera?.Shake(seconds, strength);
        }

        public float ClampX(float x)
        {
            float limit = Stage != null ? Stage.HalfWidth : 9f;
            x = Mathf.Clamp(x, -limit, limit);

            // Os dois lutadores precisam caber na tela.
            if (Camera != null && Fighters[0] != null && Fighters[1] != null)
            {
                float maxDistance = Camera.HalfViewWidth * 2f - 1.2f;
                float center = (Fighters[0].Position.x + Fighters[1].Position.x) * 0.5f;
                x = Mathf.Clamp(x, center - maxDistance * 0.5f - 0.3f, center + maxDistance * 0.5f + 0.3f);
            }

            return x;
        }

        public bool IsInsideView(float x, float margin)
        {
            if (Camera == null)
            {
                return Mathf.Abs(x) < 12f;
            }

            return Mathf.Abs(x - Camera.X) < Camera.HalfViewWidth + margin;
        }

        public void RegisterHit(KombatFighter attacker, KombatFighter victim, float damage, int combo)
        {
            if (combo >= 2)
            {
                LastComboCount = combo;
                LastComboPlayer = attacker.PlayerIndex;
                ComboTime = 1.4f;
            }
        }

        public void OnUppercutLanded(KombatFighter attacker)
        {
            Shake(0.25f, 0.25f);
            if (UnityEngine.Random.value < 0.08f)
            {
                ToastyTime = 1.2f;
                KombatAudio.Sfx("Announcer/toasty");
            }
        }

        public void OnUltimateStarted(KombatFighter fighter)
        {
            FlashColor = new Color(0.05f, 0.2f, 0.75f, 0.85f);
            FlashTime = 0.6f;
            SetBanner(KombatBanner.UltimateFlash, 1.2f, fighter.Data != null && fighter.Data.UltimateMove != null
                ? fighter.Data.UltimateMove.MoveName
                : "SUPREMO");
            AddHitstop(20);
        }

        public void OnFighterLanded(KombatFighter fighter)
        {
        }

        public void ShowLine(KombatFighter fighter, string line, float seconds)
        {
            Lines[fighter.PlayerIndex] = line;
            LineTimes[fighter.PlayerIndex] = seconds;
        }

        /// <summary>Atalho do modo treino: deixa o boneco tonto para testar Fatality e Babality.</summary>
        public void TrainingFinishTest()
        {
            if (Mode != KombatMode.Training || fatalityStarted || FinishActive)
            {
                return;
            }

            StartCoroutine(TrainingFinish());
        }

        private IEnumerator TrainingFinish()
        {
            knockedOut = Fighters[1];
            roundWinner = Fighters[0];
            knockedOut.EnterFinishDizzy();
            bool female = knockedOut.Data != null && knockedOut.Data.IsFemale;
            SetBanner(female ? KombatBanner.FinishHer : KombatBanner.FinishHim, 2f, female ? "FINISH HER!" : "FINISH HIM!");
            KombatAudio.Announce("Announcer/finish_him");
            FinishActive = true;
            float timer = 10f;
            while (timer > 0f && !fatalityStarted)
            {
                timer -= Time.deltaTime;
                if (knockedOut.State != FighterState.Dizzy && !fatalityStarted) break;
                yield return null;
            }

            FinishActive = false;
            while (fatalityStarted) yield return null;
            yield return new WaitForSeconds(0.5f);
            Fighters[0].ResetForRound(-2.6f);
            Fighters[1].ResetForRound(2.6f);
            Fighters[0].Unlock();
            Fighters[1].Unlock();
            KombatAudio.RandomStageMusic();
        }

        public bool DummyBlocks
        {
            get => trainingDummy != null && trainingDummy.AlwaysBlock;
            set
            {
                if (trainingDummy != null) trainingDummy.AlwaysBlock = value;
            }
        }

        private void SetBanner(KombatBanner banner, float seconds, string text)
        {
            Banner = banner;
            BannerTime = seconds;
            BannerText = text;
        }

        private void OnDestroy()
        {
            foreach (KombatProjectile projectile in projectiles)
            {
                if (projectile != null) Destroy(projectile.gameObject);
            }
        }
    }
}
