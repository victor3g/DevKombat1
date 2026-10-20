using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevKombat.Kombat
{
    [Serializable]
    public class KombatStageInfo
    {
        public string id;
        public string name;
        public float floor = 0.9f;
    }

    [Serializable]
    public class KombatStageCatalog
    {
        public List<KombatStageInfo> stages = new List<KombatStageInfo>();
    }

    /// <summary>
    /// Carrega os assets extraídos para Assets/Resources/Kombat (Tools/extrair_assets_mk.py).
    /// Tudo é opcional: sem os arquivos o jogo continua rodando com formas e sem som.
    /// </summary>
    public static class KombatResources
    {
        public const string Root = "Kombat/";
        public const float FighterPixelsPerUnit = 28f;

        private static readonly Dictionary<string, Sprite[]> FrameCache = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, AudioClip[]> ClipCache = new Dictionary<string, AudioClip[]>();
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static Sprite whiteSprite;
        private static Sprite placeholderFighter;
        private static Sprite shadowSprite;
        private static KombatStageCatalog stageCatalog;
        private static Font font;
        private static bool fontLoaded;

        /// <summary>Quadros de uma pasta, em ordem de nome (00.png, 01.png...).</summary>
        public static Sprite[] Frames(string folder)
        {
            if (FrameCache.TryGetValue(folder, out Sprite[] cached))
            {
                return cached;
            }

            Sprite[] frames = Resources.LoadAll<Sprite>(Root + folder);
            Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            FrameCache[folder] = frames;
            return frames;
        }

        public static bool HasExtractedAssets => Frames("Fighters/Ninja/idle").Length > 0;

        public static AudioClip Clip(string path)
        {
            AudioClip[] clips = Clips(path, true);
            return clips.Length > 0 ? clips[0] : null;
        }

        /// <summary>Todos os sons de uma pasta (ou um arquivo quando single = true).</summary>
        public static AudioClip[] Clips(string path, bool single = false)
        {
            if (ClipCache.TryGetValue(path, out AudioClip[] cached))
            {
                return cached;
            }

            AudioClip[] clips;
            if (single)
            {
                AudioClip clip = Resources.Load<AudioClip>(Root + "Audio/" + path);
                clips = clip != null ? new[] { clip } : Array.Empty<AudioClip>();
            }
            else
            {
                clips = Resources.LoadAll<AudioClip>(Root + "Audio/" + path);
                Array.Sort(clips, (a, b) => string.CompareOrdinal(a.name, b.name));
            }

            ClipCache[path] = clips;
            return clips;
        }

        public static AudioClip RandomClip(string folder)
        {
            AudioClip[] clips = Clips(folder);
            return clips.Length > 0 ? clips[UnityEngine.Random.Range(0, clips.Length)] : null;
        }

        public static Texture2D Texture(string path)
        {
            if (TextureCache.TryGetValue(path, out Texture2D cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>(Root + path);
            TextureCache[path] = texture;
            return texture;
        }

        public static Sprite StageSprite(string id)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(Root + "Stages/" + id);
            return sprites.Length > 0 ? sprites[0] : null;
        }

        public static KombatStageCatalog Stages
        {
            get
            {
                if (stageCatalog != null)
                {
                    return stageCatalog;
                }

                TextAsset json = Resources.Load<TextAsset>(Root + "Stages/stages");
                stageCatalog = json != null
                    ? JsonUtility.FromJson<KombatStageCatalog>(json.text)
                    : new KombatStageCatalog();
                if (stageCatalog.stages.Count == 0)
                {
                    stageCatalog.stages.Add(new KombatStageInfo { id = "", name = "Arena de Treino", floor = 0.9f });
                }

                return stageCatalog;
            }
        }

        public static Font Font
        {
            get
            {
                if (!fontLoaded)
                {
                    font = Resources.Load<Font>(Root + "Fonts/mk2");
                    fontLoaded = true;
                }

                return font;
            }
        }

        /// <summary>Quadrado branco de 1 unidade, usado para formas e placeholders.</summary>
        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                {
                    Texture2D texture = new Texture2D(4, 4) { filterMode = FilterMode.Point, name = "KombatWhite" };
                    Color[] pixels = new Color[16];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                    texture.SetPixels(pixels);
                    texture.Apply();
                    whiteSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                }

                return whiteSprite;
            }
        }

        /// <summary>Silhueta simples usada quando os assets do MK ainda não foram extraídos.</summary>
        public static Sprite PlaceholderFighter
        {
            get
            {
                if (placeholderFighter == null)
                {
                    const int width = 16;
                    const int height = 48;
                    Texture2D texture = new Texture2D(width, height) { filterMode = FilterMode.Point, name = "KombatPlaceholder" };
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            bool head = y > 38 && x > 4 && x < 11;
                            bool body = y > 18 && y <= 38 && x > 2 && x < 13;
                            bool legs = y <= 18 && (x > 2 && x < 7 || x > 8 && x < 13);
                            texture.SetPixel(x, y, head || body || legs ? Color.white : Color.clear);
                        }
                    }

                    texture.Apply();
                    placeholderFighter = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), height / 3.6f);
                }

                return placeholderFighter;
            }
        }

        public static Sprite Shadow
        {
            get
            {
                if (shadowSprite == null)
                {
                    const int width = 32;
                    const int height = 8;
                    Texture2D texture = new Texture2D(width, height) { filterMode = FilterMode.Bilinear, name = "KombatShadow" };
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            float dx = (x + 0.5f - width / 2f) / (width / 2f);
                            float dy = (y + 0.5f - height / 2f) / (height / 2f);
                            float alpha = Mathf.Clamp01(1f - (dx * dx + dy * dy)) * 0.55f;
                            texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                        }
                    }

                    texture.Apply();
                    shadowSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 20f);
                }

                return shadowSprite;
            }
        }
    }
}
