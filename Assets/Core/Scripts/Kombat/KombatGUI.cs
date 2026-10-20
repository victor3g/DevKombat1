using DevKombat.Fighters;
using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>Desenho da interface (IMGUI) em uma tela virtual de 1280x720.</summary>
    public static class KombatGUI
    {
        public const float Width = 1280f;
        public const float Height = 720f;

        public static readonly Color MkYellow = new Color(1f, 0.85f, 0.1f);
        public static readonly Color MkRed = new Color(0.8f, 0.05f, 0.05f);
        public static readonly Color MkGreen = new Color(0.1f, 0.75f, 0.15f);

        private static GUIStyle textStyle;
        private static float scale = 1f;
        private static Vector2 offset;

        public static void Begin()
        {
            scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            offset = new Vector2((Screen.width - Width * scale) * 0.5f, (Screen.height - Height * scale) * 0.5f);
            GUI.matrix = Matrix4x4.TRS(new Vector3(offset.x, offset.y, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        public static Vector2 WorldToGui(Camera camera, Vector3 world)
        {
            Vector3 screen = camera.WorldToScreenPoint(world);
            return new Vector2((screen.x - offset.x) / scale, (Screen.height - screen.y - offset.y) / scale);
        }

        private static GUIStyle Style
        {
            get
            {
                if (textStyle == null)
                {
                    textStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = false,
                        richText = false,
                        clipping = TextClipping.Overflow
                    };
                    Font font = KombatResources.Font;
                    if (font != null)
                    {
                        textStyle.font = font;
                    }
                }

                return textStyle;
            }
        }

        public static void Text(Rect rect, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter,
            bool shadow = true, bool wrap = false)
        {
            GUIStyle style = Style;
            style.fontSize = size;
            style.alignment = anchor;
            style.wordWrap = wrap;
            if (shadow)
            {
                style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.85f);
                GUI.Label(new Rect(rect.x + size * 0.08f + 1f, rect.y + size * 0.08f + 1f, rect.width, rect.height), text, style);
            }

            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        public static void Box(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        public static void Frame(Rect rect, Color color, float thickness)
        {
            Box(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Box(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Box(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Box(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        /// <summary>Desenha uma imagem pixelada mantendo a proporção, centralizada no retângulo.</summary>
        public static bool Image(Rect rect, string path, Color? tint = null)
        {
            Texture2D texture = KombatResources.Texture(path);
            if (texture == null)
            {
                return false;
            }

            Color previous = GUI.color;
            GUI.color = tint ?? Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
            return true;
        }

        public static void Sprite(Rect rect, Sprite sprite, Color color, bool flip = false, bool fitBottom = true)
        {
            if (sprite == null)
            {
                return;
            }

            Texture2D texture = sprite.texture;
            Rect uv = sprite.textureRect;
            Rect coords = new Rect(uv.x / texture.width, uv.y / texture.height, uv.width / texture.width, uv.height / texture.height);
            float aspect = uv.width / uv.height;
            float height = rect.height;
            float width = height * aspect;
            if (width > rect.width)
            {
                width = rect.width;
                height = width / aspect;
            }

            Rect target = new Rect(rect.center.x - width * 0.5f, fitBottom ? rect.yMax - height : rect.center.y - height * 0.5f, width, height);
            if (flip)
            {
                coords = new Rect(coords.xMax, coords.y, -coords.width, coords.height);
            }

            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(target, texture, coords, true);
            GUI.color = previous;
        }

        /// <summary>Retrato do lutador: imagem própria ou o ninja tingido com a cor do lutador.</summary>
        public static void Portrait(Rect rect, FighterData data, float time, bool flip, Color? costumeOverride = null)
        {
            if (data == null)
            {
                return;
            }

            if (data.SelectionPortrait != null)
            {
                Sprite(rect, data.SelectionPortrait, Color.white, flip, false);
                return;
            }

            KombatAnimationLibrary library = LibraryFor(data);
            KombatFrames frames = library.Get("idle");
            if (frames.Count == 0)
            {
                Sprite(rect, KombatResources.PlaceholderFighter, costumeOverride ?? data.CostumeColor, flip);
                return;
            }

            int count = frames.Count;
            int cycle = Mathf.Max(1, count * 2 - 2);
            int step = Mathf.FloorToInt(time * 8f) % cycle;
            int index = step < count ? step : cycle - step;
            Sprite(rect, frames.Body[index], Color.white, flip);
            if (!frames.Custom && frames.Costume != null && index < frames.Costume.Length)
            {
                Sprite(rect, frames.Costume[index], costumeOverride ?? data.CostumeColor, flip);
            }
        }

        private static readonly System.Collections.Generic.Dictionary<FighterData, KombatAnimationLibrary> Libraries =
            new System.Collections.Generic.Dictionary<FighterData, KombatAnimationLibrary>();

        private static KombatAnimationLibrary LibraryFor(FighterData data)
        {
            if (!Libraries.TryGetValue(data, out KombatAnimationLibrary library))
            {
                library = new KombatAnimationLibrary(data);
                Libraries[data] = library;
            }

            return library;
        }

        /// <summary>Barra de vida no estilo do MK2: borda amarela, fundo vermelho e verde por cima.</summary>
        public static void HealthBar(Rect rect, float ratio, bool rightSide, string name)
        {
            Box(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), MkYellow);
            Box(rect, MkRed);
            float width = rect.width * Mathf.Clamp01(ratio);
            Rect fill = rightSide ? new Rect(rect.xMax - width, rect.y, width, rect.height) : new Rect(rect.x, rect.y, width, rect.height);
            Box(fill, MkGreen);
            Text(new Rect(rect.x + 10f, rect.y, rect.width - 20f, rect.height), name.ToUpperInvariant(), 20, MkYellow,
                rightSide ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
        }

        public static void MeterBar(Rect rect, float ratio, bool rightSide)
        {
            Box(rect, new Color(0f, 0f, 0f, 0.6f));
            float width = rect.width * Mathf.Clamp01(ratio);
            Rect fill = rightSide ? new Rect(rect.xMax - width, rect.y, width, rect.height) : new Rect(rect.x, rect.y, width, rect.height);
            Color color = ratio >= 1f ? Color.Lerp(new Color(0.2f, 0.6f, 1f), Color.white, Mathf.PingPong(Time.time * 3f, 1f)) : new Color(0.2f, 0.5f, 1f);
            Box(fill, color);
        }
    }
}
