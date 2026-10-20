using System.Collections.Generic;
using DevKombat.Fighters;
using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>
    /// Fluxo completo do jogo: título, seleção de lutadores, cenário, tela de VS,
    /// luta e volta para a seleção. Coloque este componente em uma cena vazia
    /// (o menu Dev Kombat > Preparar Kombat faz isso).
    /// </summary>
    public class KombatGame : MonoBehaviour
    {
        private enum Screen
        {
            Title,
            Select,
            Stage,
            Versus,
            Fight
        }

        [SerializeField] private KombatRoster roster;
        [Range(0f, 1f)] [SerializeField] private float cpuDifficulty = 0.5f;
        [SerializeField] private bool blood = true;

        private readonly KombatKeyMap keysOne = KombatKeyMap.PlayerOne();
        private readonly KombatKeyMap keysTwo = KombatKeyMap.PlayerTwo();
        private readonly int[] cursor = new int[2];
        private readonly bool[] chosen = new bool[2];

        private List<FighterData> fighters = new List<FighterData>();
        private Screen screen = Screen.Title;
        private KombatMode mode = KombatMode.VersusCpu;
        private int titleIndex;
        private int stageIndex;
        private float screenTime;
        private float versusTimer;
        private KombatMatch match;
        private int pauseCursor;
        private bool cpuRolling;
        private float cpuRollTimer;

        private static readonly string[] TitleOptions = { "1 JOGADOR x CPU", "2 JOGADORES", "TREINO", "SAIR" };

        private void Start()
        {
            Application.targetFrameRate = 60;
            KombatFX.BloodEnabled = blood;
            fighters = roster != null ? roster.BuildList() : KombatTemplates.Create();
            if (fighters.Count == 0)
            {
                fighters = KombatTemplates.Create();
            }

            KombatCamera.Create();
            if (!KombatResources.HasExtractedAssets)
            {
                Debug.LogWarning("Dev Kombat: assets do MK não encontrados. Rode Tools/extrair_assets_mk.py " +
                                 "para ter cenários, sons e efeitos. O jogo funciona com formas simples até lá.");
            }

            GoTo(Screen.Title);
        }

        private void GoTo(Screen next)
        {
            screen = next;
            screenTime = 0f;
            switch (next)
            {
                case Screen.Title:
                    KombatAudio.Music("Music/title");
                    break;
                case Screen.Select:
                    chosen[0] = chosen[1] = false;
                    cpuRolling = false;
                    KombatAudio.Music("Music/select");
                    break;
                case Screen.Versus:
                    versusTimer = 3f;
                    KombatAudio.StopMusic();
                    KombatAudio.Sfx("Announcer/gong");
                    break;
            }
        }

        private void Update()
        {
            screenTime += Time.deltaTime;
            switch (screen)
            {
                case Screen.Title:
                    UpdateTitle();
                    break;
                case Screen.Select:
                    UpdateSelect();
                    break;
                case Screen.Stage:
                    UpdateStage();
                    break;
                case Screen.Versus:
                    versusTimer -= Time.deltaTime;
                    if (versusTimer <= 0f || screenTime > 0.6f && Confirm(0))
                    {
                        StartFight();
                    }

                    break;
                case Screen.Fight:
                    UpdateFight();
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Controles de menu
        // ------------------------------------------------------------------

        private bool Confirm(int player)
        {
            KombatKeyMap keys = player == 0 ? keysOne : keysTwo;
            if (player == 0)
            {
                return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) && mode != KombatMode.VersusPlayer ||
                       Input.GetKeyDown(keys.lowPunch) || Input.GetKeyDown(keys.highPunch);
            }

            return Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(keys.lowPunch) || Input.GetKeyDown(keys.highPunch);
        }

        private bool Back()
        {
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
        }

        private Vector2Int Navigate(int player)
        {
            KombatKeyMap keys = player == 0 ? keysOne : keysTwo;
            Vector2Int move = Vector2Int.zero;
            if (Input.GetKeyDown(keys.left)) move.x -= 1;
            if (Input.GetKeyDown(keys.right)) move.x += 1;
            if (Input.GetKeyDown(keys.up)) move.y -= 1;
            if (Input.GetKeyDown(keys.down)) move.y += 1;
            if (player == 0 && mode != KombatMode.VersusPlayer)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) move.x -= 1;
                if (Input.GetKeyDown(KeyCode.RightArrow)) move.x += 1;
                if (Input.GetKeyDown(KeyCode.UpArrow)) move.y -= 1;
                if (Input.GetKeyDown(KeyCode.DownArrow)) move.y += 1;
            }

            return move;
        }

        private void UpdateTitle()
        {
            Vector2Int move = Navigate(0);
            if (move.y != 0)
            {
                titleIndex = (titleIndex + move.y + TitleOptions.Length) % TitleOptions.Length;
                KombatAudio.RandomSfx("Hits", 0.3f, 1.6f);
            }

            if (screenTime > 0.3f && (Confirm(0) || Input.GetKeyDown(KeyCode.Space)))
            {
                switch (titleIndex)
                {
                    case 0: mode = KombatMode.VersusCpu; break;
                    case 1: mode = KombatMode.VersusPlayer; break;
                    case 2: mode = KombatMode.Training; break;
                    default:
                        Application.Quit();
                        return;
                }

                GoTo(Screen.Select);
            }
        }

        private const int Columns = 4;

        private void UpdateSelect()
        {
            if (Back())
            {
                GoTo(Screen.Title);
                return;
            }

            bool versusPlayer = mode == KombatMode.VersusPlayer;
            for (int player = 0; player < (versusPlayer ? 2 : 1); player++)
            {
                if (chosen[player])
                {
                    continue;
                }

                Vector2Int move = Navigate(player);
                if (move != Vector2Int.zero)
                {
                    int count = fighters.Count;
                    int index = cursor[player] + move.x + move.y * Columns;
                    cursor[player] = (index % count + count) % count;
                    KombatAudio.RandomSfx("Hits", 0.25f, 1.7f);
                }

                if (screenTime > 0.3f && Confirm(player))
                {
                    chosen[player] = true;
                    KombatAudio.Sfx("Announcer/gong", 0.6f);
                    if (!versusPlayer)
                    {
                        cpuRolling = true;
                        cpuRollTimer = mode == KombatMode.Training ? 0.2f : 1.4f;
                    }
                }
            }

            if (cpuRolling)
            {
                cpuRollTimer -= Time.deltaTime;
                if (cpuRollTimer > 0f)
                {
                    if (Time.frameCount % 4 == 0)
                    {
                        cursor[1] = Random.Range(0, fighters.Count);
                    }
                }
                else
                {
                    if (mode == KombatMode.Training)
                    {
                        cursor[1] = cursor[0];
                    }

                    cpuRolling = false;
                    chosen[1] = true;
                }
            }

            if (chosen[0] && chosen[1] && !cpuRolling)
            {
                GoTo(Screen.Stage);
            }
        }

        private void UpdateStage()
        {
            List<KombatStageInfo> stages = KombatResources.Stages.stages;
            if (Back())
            {
                GoTo(Screen.Select);
                return;
            }

            Vector2Int move = Navigate(0);
            int total = stages.Count + 1;
            if (move.x != 0 || move.y != 0)
            {
                stageIndex = (stageIndex + move.x + move.y + total) % total;
            }

            if (screenTime > 0.3f && Confirm(0))
            {
                GoTo(Screen.Versus);
            }
        }

        private KombatStageInfo SelectedStage()
        {
            List<KombatStageInfo> stages = KombatResources.Stages.stages;
            if (stageIndex == 0 || stageIndex > stages.Count)
            {
                return stages[Random.Range(0, stages.Count)];
            }

            return stages[stageIndex - 1];
        }

        private void StartFight()
        {
            FighterData one = fighters[cursor[0]];
            FighterData two = fighters[cursor[1]];
            IKombatInputSource inputOne = new KeyboardKombatInput(keysOne);
            IKombatInputSource inputTwo;
            switch (mode)
            {
                case KombatMode.VersusPlayer:
                    inputTwo = new KeyboardKombatInput(keysTwo);
                    break;
                case KombatMode.Training:
                    inputTwo = new DummyKombatInput();
                    break;
                default:
                    inputTwo = new KombatAI(cpuDifficulty);
                    break;
            }

            match = KombatMatch.Create(one, two, inputOne, inputTwo, SelectedStage(), mode);
            match.OnMatchFinished += _ => EndFight();
            screen = Screen.Fight;
            screenTime = 0f;
        }

        private void EndFight()
        {
            if (match != null)
            {
                Destroy(match.gameObject);
                match = null;
            }

            KombatEffect.ClearAll();
            Time.timeScale = 1f;
            AudioListener.pause = false;

            KombatCamera.Create().Snap(0f);
            GoTo(Screen.Select);
        }

        private void UpdateFight()
        {
            if (match == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) && match.Paused)
            {
                match.Paused = !match.Paused;
                AudioListener.pause = match.Paused;
                Time.timeScale = match.Paused ? 0f : 1f;
                pauseCursor = 0;
            }

            if (match.Paused && Input.GetKeyDown(KeyCode.Q))
            {
                match.Paused = false;
                EndFight();
                return;
            }

            if (mode == KombatMode.Training && !match.Paused)
            {
                if (Input.GetKeyDown(KeyCode.F1)) match.TrainingFinishTest();
                if (Input.GetKeyDown(KeyCode.F2)) match.DummyBlocks = !match.DummyBlocks;
            }

            if (match.Paused)
            {
                Vector2Int move = Navigate(0);
                pauseCursor = Mathf.Clamp(pauseCursor + move.x, 0, 1);
            }
        }

        // ------------------------------------------------------------------
        // Desenho
        // ------------------------------------------------------------------

        private void OnGUI()
        {
            KombatGUI.Begin();
            switch (screen)
            {
                case Screen.Title:
                    DrawTitle();
                    break;
                case Screen.Select:
                    DrawSelect();
                    break;
                case Screen.Stage:
                    DrawStage();
                    break;
                case Screen.Versus:
                    DrawVersus();
                    break;
                case Screen.Fight:
                    DrawFight();
                    break;
            }
        }

        private void DrawBackdrop()
        {
            KombatGUI.Box(new Rect(0f, 0f, KombatGUI.Width, KombatGUI.Height), new Color(0.05f, 0.02f, 0.02f));
            if (!KombatGUI.Image(new Rect(0f, 0f, KombatGUI.Width, KombatGUI.Height), "UI/panel_stone", new Color(0.55f, 0.55f, 0.55f)))
            {
                KombatGUI.Box(new Rect(0f, 0f, KombatGUI.Width, KombatGUI.Height), new Color(0.2f, 0.2f, 0.22f));
            }
        }

        private void DrawTitle()
        {
            KombatGUI.Box(new Rect(0f, 0f, KombatGUI.Width, KombatGUI.Height), Color.black);
            float pulse = 0.85f + Mathf.Sin(screenTime * 2f) * 0.15f;
            if (!KombatGUI.Image(new Rect(390f, 40f, 500f, 380f), "UI/dragon_disc", new Color(pulse, pulse, pulse)))
            {
                KombatGUI.Box(new Rect(540f, 120f, 200f, 200f), KombatGUI.MkRed);
            }

            KombatGUI.Text(new Rect(0f, 400f, KombatGUI.Width, 80f), "DEV KOMBAT", 72, KombatGUI.MkYellow);
            for (int i = 0; i < TitleOptions.Length; i++)
            {
                bool selected = i == titleIndex;
                Color color = selected ? Color.Lerp(KombatGUI.MkYellow, Color.white, Mathf.PingPong(screenTime * 3f, 1f)) : new Color(0.7f, 0.1f, 0.1f);
                KombatGUI.Text(new Rect(0f, 490f + i * 46f, KombatGUI.Width, 44f), (selected ? "> " : "") + TitleOptions[i] + (selected ? " <" : ""), 32, color);
            }

            KombatGUI.Text(new Rect(0f, 680f, KombatGUI.Width, 30f), "W/S ou setas para escolher · Enter ou J para confirmar", 16,
                new Color(0.8f, 0.8f, 0.8f), shadow: false);
        }

        private void DrawSelect()
        {
            DrawBackdrop();
            KombatGUI.Text(new Rect(0f, 14f, KombatGUI.Width, 50f), "ESCOLHA SEU LUTADOR", 40, KombatGUI.MkYellow);

            const float cell = 112f;
            const float gap = 10f;
            int rows = Mathf.CeilToInt(fighters.Count / (float)Columns);
            float gridWidth = Columns * cell + (Columns - 1) * gap;
            float startX = (KombatGUI.Width - gridWidth) * 0.5f;
            float startY = 90f;

            for (int i = 0; i < fighters.Count; i++)
            {
                int col = i % Columns;
                int row = i / Columns;
                Rect rect = new Rect(startX + col * (cell + gap), startY + row * (cell + gap), cell, cell);
                KombatGUI.Box(rect, new Color(0.08f, 0.06f, 0.06f, 0.95f));
                KombatGUI.Portrait(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, rect.height - 12f), fighters[i], 0f, false);
                bool one = cursor[0] == i;
                bool two = (mode == KombatMode.VersusPlayer || chosen[0]) && cursor[1] == i;
                if (one) KombatGUI.Frame(rect, Color.Lerp(new Color(0.2f, 0.4f, 1f), Color.white, chosen[0] ? 0f : Mathf.PingPong(Time.time * 4f, 0.6f)), 5f);
                if (two) KombatGUI.Frame(new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f),
                    Color.Lerp(KombatGUI.MkRed, Color.white, chosen[1] ? 0f : Mathf.PingPong(Time.time * 4f, 0.6f)), 5f);
            }

            DrawSelectSide(0, new Rect(30f, 90f, 330f, 520f));
            if (mode == KombatMode.VersusPlayer || chosen[0])
            {
                DrawSelectSide(1, new Rect(KombatGUI.Width - 360f, 90f, 330f, 520f));
            }

            string help = mode == KombatMode.VersusPlayer
                ? "P1: WASD + J  ·  P2: setas + Num1  ·  Esc volta"
                : "WASD ou setas para escolher · J/Enter confirma · Esc volta";
            KombatGUI.Text(new Rect(0f, 680f, KombatGUI.Width, 30f), help, 16, new Color(0.9f, 0.9f, 0.9f));
            if (rows > 4)
            {
                KombatGUI.Text(new Rect(0f, 650f, KombatGUI.Width, 30f), "(role com cima/baixo)", 14, Color.gray);
            }
        }

        private void DrawSelectSide(int player, Rect rect)
        {
            FighterData data = fighters[Mathf.Clamp(cursor[player], 0, fighters.Count - 1)];
            Color label = player == 0 ? new Color(0.4f, 0.6f, 1f) : KombatGUI.MkRed;
            KombatGUI.Text(new Rect(rect.x, rect.y, rect.width, 30f), player == 0 ? "JOGADOR 1" : mode == KombatMode.VersusPlayer ? "JOGADOR 2" : "CPU", 22, label);
            KombatGUI.Portrait(new Rect(rect.x + 40f, rect.y + 40f, rect.width - 80f, 300f), data, Time.time, player == 1);
            KombatGUI.Text(new Rect(rect.x, rect.y + 350f, rect.width, 40f), data.FighterName.ToUpperInvariant(), 30, KombatGUI.MkYellow);
            KombatGUI.Text(new Rect(rect.x, rect.y + 388f, rect.width, 26f), data.FighterTitle ?? "", 18, Color.white);
            KombatGUI.Text(new Rect(rect.x + 10f, rect.y + 418f, rect.width - 20f, 100f), data.SelectionDescription ?? "", 15,
                new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperCenter, false, true);
        }

        private void DrawStage()
        {
            DrawBackdrop();
            List<KombatStageInfo> stages = KombatResources.Stages.stages;
            KombatGUI.Text(new Rect(0f, 20f, KombatGUI.Width, 50f), "ESCOLHA A ARENA", 40, KombatGUI.MkYellow);
            Rect preview = new Rect(190f, 100f, 900f, 440f);
            KombatGUI.Box(preview, Color.black);
            string name;
            if (stageIndex == 0)
            {
                name = "ALEATÓRIA";
                KombatGUI.Text(preview, "?", 200, KombatGUI.MkRed);
            }
            else
            {
                KombatStageInfo info = stages[Mathf.Clamp(stageIndex - 1, 0, stages.Count - 1)];
                name = info.name.ToUpperInvariant();
                if (!KombatGUI.Image(preview, "Stages/" + info.id))
                {
                    KombatGUI.Text(preview, "(sem imagem)", 30, Color.gray);
                }
            }

            KombatGUI.Frame(preview, KombatGUI.MkYellow, 4f);
            KombatGUI.Text(new Rect(0f, 560f, KombatGUI.Width, 50f), "< " + name + " >", 36, Color.white);
            KombatGUI.Text(new Rect(0f, 680f, KombatGUI.Width, 30f), "A/D ou setas trocam · J/Enter confirma · Esc volta", 16,
                new Color(0.9f, 0.9f, 0.9f));
        }

        private void DrawVersus()
        {
            KombatGUI.Box(new Rect(0f, 0f, KombatGUI.Width, KombatGUI.Height), Color.black);
            KombatGUI.Image(new Rect(240f, 40f, 800f, 640f), "UI/versus_stone");
            FighterData one = fighters[cursor[0]];
            FighterData two = fighters[cursor[1]];
            Color? mirror = null;
            if (one == two)
            {
                Color.RGBToHSV(one.CostumeColor, out float h, out float s, out float v);
                mirror = Color.HSVToRGB((h + 0.45f) % 1f, Mathf.Max(0.5f, s), Mathf.Max(0.6f, v));
            }

            KombatGUI.Portrait(new Rect(330f, 190f, 230f, 300f), one, Time.time, false);
            KombatGUI.Portrait(new Rect(720f, 190f, 230f, 300f), two, Time.time, true, mirror);
            KombatGUI.Text(new Rect(240f, 520f, 400f, 40f), one.FighterName.ToUpperInvariant(), 28, KombatGUI.MkYellow);
            KombatGUI.Text(new Rect(640f, 520f, 400f, 40f), two.FighterName.ToUpperInvariant(), 28, KombatGUI.MkYellow);
            KombatGUI.Text(new Rect(0f, 300f, KombatGUI.Width, 80f), "VS", 64, KombatGUI.MkRed);
            KombatGUI.Text(new Rect(0f, 600f, KombatGUI.Width, 40f), "ARENA: " + SelectedStageName(), 22, new Color(0.3f, 1f, 0.3f));
        }

        private string SelectedStageName()
        {
            if (stageIndex == 0) return "ALEATÓRIA";
            List<KombatStageInfo> stages = KombatResources.Stages.stages;
            return stages[Mathf.Clamp(stageIndex - 1, 0, stages.Count - 1)].name.ToUpperInvariant();
        }

        private void DrawFight()
        {
            if (match == null)
            {
                return;
            }

            KombatFighter one = match.Fighters[0];
            KombatFighter two = match.Fighters[1];

            // Barras de vida e nomes.
            KombatGUI.HealthBar(new Rect(40f, 30f, 500f, 30f), one.Health / one.MaxHealth, false, one.DisplayName);
            KombatGUI.HealthBar(new Rect(KombatGUI.Width - 540f, 30f, 500f, 30f), two.Health / two.MaxHealth, true, two.DisplayName);
            if (one.Data != null && one.Data.UltimateMove != null && one.Data.UltimateMove.IsConfigured || mode == KombatMode.Training)
                KombatGUI.MeterBar(new Rect(40f, 68f, 260f, 8f), one.Meter / 100f, false);
            if (two.Data != null && two.Data.UltimateMove != null && two.Data.UltimateMove.IsConfigured)
                KombatGUI.MeterBar(new Rect(KombatGUI.Width - 300f, 68f, 260f, 8f), two.Meter / 100f, true);

            // Medalhas de vitória.
            for (int p = 0; p < 2; p++)
            {
                for (int w = 0; w < match.Wins[p]; w++)
                {
                    float x = p == 0 ? 40f + w * 32f : KombatGUI.Width - 70f - w * 32f;
                    Rect medal = new Rect(x, 82f, 28f, 30f);
                    if (!KombatGUI.Image(medal, "UI/win_medal"))
                    {
                        KombatGUI.Box(medal, KombatGUI.MkYellow);
                    }
                }
            }

            if (mode != KombatMode.Training)
            {
                KombatGUI.Text(new Rect(KombatGUI.Width * 0.5f - 60f, 20f, 120f, 56f), Mathf.CeilToInt(match.TimeLeft).ToString("00"), 46,
                    match.TimeLeft < 10f ? KombatGUI.MkRed : KombatGUI.MkYellow);
            }

            DrawBanner();
            DrawCombo();
            DrawLines();
            DrawToasty();

            if (match.FlashTime > 0f)
            {
                Color flash = match.FlashColor;
                flash.a *= Mathf.Clamp01(match.FlashTime / 0.6f);
                KombatGUI.Box(new Rect(0f, 0f, KombatGUI.Width, KombatGUI.Height), flash);
            }

            if (mode == KombatMode.Training)
            {
                KombatGUI.Text(new Rect(20f, 680f, KombatGUI.Width - 40f, 30f),
                    "TREINO · F1 testar Fatality/Babality · F2 boneco " + (match.DummyBlocks ? "defende" : "parado") + " · Esc lista de golpes",
                    16, Color.white, TextAnchor.MiddleLeft);
            }

            if (match.Paused)
            {
                DrawPause();
            }
        }

        private void DrawBanner()
        {
            if (match.BannerTime <= 0f || match.Banner == KombatBanner.None)
            {
                return;
            }

            Rect area = new Rect(240f, 200f, 800f, 160f);
            bool flashRed = Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
            bool drawn = false;
            switch (match.Banner)
            {
                case KombatBanner.Round:
                    drawn = KombatGUI.Image(new Rect(360f, 220f, 380f, 110f), "UI/round");
                    if (drawn)
                    {
                        KombatGUI.Text(new Rect(740f, 210f, 160f, 130f), match.Round.ToString(), 110, KombatGUI.MkYellow);
                    }

                    break;
                case KombatBanner.Fight:
                    drawn = match.BannerText == "FIGHT!" && KombatGUI.Image(area, flashRed ? "UI/fight_red" : "UI/fight_yellow");
                    break;
                case KombatBanner.FinishHim:
                    drawn = KombatGUI.Image(area, flashRed ? "UI/finish_him_red" : "UI/finish_him_yellow");
                    break;
                case KombatBanner.FinishHer:
                    drawn = KombatGUI.Image(area, flashRed ? "UI/finish_her_red" : "UI/finish_her_yellow");
                    break;
                case KombatBanner.Fatality:
                    drawn = KombatGUI.Image(new Rect(240f, 150f, 800f, 220f), "UI/fatality_big");
                    break;
                case KombatBanner.FlawlessVictory:
                    drawn = KombatGUI.Image(new Rect(240f, 300f, 800f, 70f), "UI/flawless_victory");
                    break;
                case KombatBanner.TimeIsUp:
                    drawn = KombatGUI.Image(new Rect(340f, 260f, 600f, 80f), "UI/time_is_up");
                    break;
            }

            if (!drawn)
            {
                int size = match.Banner == KombatBanner.Wins || match.Banner == KombatBanner.UltimateFlash ? 54 : 80;
                Color color = match.Banner == KombatBanner.Babality ? new Color(0.4f, 1f, 0.5f) :
                    flashRed ? KombatGUI.MkRed : KombatGUI.MkYellow;
                KombatGUI.Text(new Rect(0f, 220f, KombatGUI.Width, 120f), match.BannerText, size, color);
            }
        }

        private void DrawCombo()
        {
            if (match.ComboTime <= 0f || match.LastComboCount < 2)
            {
                return;
            }

            bool left = match.LastComboPlayer == 0;
            Rect rect = left ? new Rect(40f, 140f, 400f, 60f) : new Rect(KombatGUI.Width - 440f, 140f, 400f, 60f);
            KombatGUI.Text(rect, match.LastComboCount + " HITS", 40, KombatGUI.MkYellow, left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
        }

        private void DrawLines()
        {
            Camera camera = match.Camera != null ? match.Camera.Camera : null;
            if (camera == null)
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                if (match.LineTimes[i] <= 0f || string.IsNullOrEmpty(match.Lines[i]))
                {
                    continue;
                }

                KombatFighter fighter = match.Fighters[i];
                Vector2 head = KombatGUI.WorldToGui(camera, new Vector3(fighter.Position.x, fighter.Position.y + 4.1f, 0f));
                Rect bubble = new Rect(Mathf.Clamp(head.x - 170f, 10f, KombatGUI.Width - 350f), Mathf.Max(120f, head.y - 70f), 340f, 64f);
                KombatGUI.Box(bubble, new Color(1f, 1f, 1f, 0.92f));
                KombatGUI.Frame(bubble, Color.black, 3f);
                KombatGUI.Text(new Rect(bubble.x + 10f, bubble.y + 4f, bubble.width - 20f, bubble.height - 8f), match.Lines[i], 17,
                    Color.black, TextAnchor.MiddleCenter, false, true);
            }
        }

        private void DrawToasty()
        {
            if (match.ToastyTime <= 0f)
            {
                return;
            }

            float t = match.ToastyTime;
            float slide = t > 1f ? (1.2f - t) / 0.2f : t < 0.2f ? t / 0.2f : 1f;
            Rect face = new Rect(KombatGUI.Width - 160f * slide, KombatGUI.Height - 220f, 150f, 200f);
            KombatGUI.Image(face, "UI/toasty_face");
            KombatGUI.Image(new Rect(face.x - 40f, face.y - 40f, 200f, 40f), "UI/toasty");
        }

        private void DrawPause()
        {
            KombatGUI.Box(new Rect(0f, 0f, KombatGUI.Width, KombatGUI.Height), new Color(0f, 0f, 0f, 0.82f));
            KombatGUI.Text(new Rect(0f, 20f, KombatGUI.Width, 50f), "PAUSA - LISTA DE GOLPES", 36, KombatGUI.MkYellow);
            KombatFighter fighter = match.Fighters[pauseCursor];
            KombatGUI.Text(new Rect(0f, 70f, KombatGUI.Width, 40f), "< " + fighter.DisplayName.ToUpperInvariant() + " >", 28, Color.white);

            float y = 120f;
            KombatGUI.Text(new Rect(140f, y, 1000f, 28f), "GOLPES BÁSICOS", 20, KombatGUI.MkRed, TextAnchor.MiddleLeft);
            y += 30f;
            foreach ((string command, string name) in KombatMoves.MoveList)
            {
                MoveLine(ref y, command, name);
            }

            y += 10f;
            KombatGUI.Text(new Rect(140f, y, 1000f, 28f), "ESPECIAIS", 20, KombatGUI.MkRed, TextAnchor.MiddleLeft);
            y += 30f;
            foreach ((FighterSpecialMove move, KombatCommand command, AbilityPreset preset) in fighter.SpecialList())
            {
                MoveLine(ref y, move.InputCommand, move.MoveName + (preset.Kind == AbilityKind.Ultimate ? " (barra cheia)" : ""));
            }

            y += 10f;
            KombatGUI.Text(new Rect(140f, y, 1000f, 28f), "FINALIZAÇÕES (durante FINISH HIM/HER)", 20, KombatGUI.MkRed, TextAnchor.MiddleLeft);
            y += 30f;
            FighterSpecialMove fatality = match.FatalityFor(fighter);
            MoveLine(ref y, fatality.InputCommand + " (perto)", "Fatality: " + fatality.MoveName);
            MoveLine(ref y, KombatMatch.BabalityCommand, "Babality");

            KombatGUI.Text(new Rect(0f, 650f, KombatGUI.Width, 30f), "A/D troca o lutador · Esc ou Enter volta · Q sai para a seleção", 16,
                new Color(0.85f, 0.85f, 0.85f));
            KombatGUI.Text(new Rect(0f, 680f, KombatGUI.Width, 30f), "P1: " + keysOne.Describe(), 13, new Color(0.7f, 0.7f, 0.7f));
        }

        private static void MoveLine(ref float y, string command, string name)
        {
            KombatGUI.Text(new Rect(160f, y, 520f, 26f), command, 17, KombatGUI.MkYellow, TextAnchor.MiddleLeft);
            KombatGUI.Text(new Rect(700f, y, 520f, 26f), name, 17, Color.white, TextAnchor.MiddleLeft);
            y += 26f;
        }
    }
}
