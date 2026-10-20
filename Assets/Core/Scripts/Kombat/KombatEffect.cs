using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>Animação de efeito que roda sozinha e se destrói no fim.</summary>
    public class KombatEffect : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<KombatEffect> Active =
            new System.Collections.Generic.List<KombatEffect>();

        /// <summary>Remove todos os efeitos da tela (fim da luta).</summary>
        public static void ClearAll()
        {
            foreach (KombatEffect effect in Active.ToArray())
            {
                if (effect != null) Destroy(effect.gameObject);
            }

            Active.Clear();
        }

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        private Sprite[] frames;
        private SpriteRenderer spriteRenderer;
        private float fps = 15f;
        private float timer;
        private bool loop;
        private float lifetime;
        private float age;
        private Vector3 velocity;
        private float gravity;
        private float spin;
        private bool bounce;
        private float fadeOut;
        private Color baseColor = Color.white;

        public SpriteRenderer Renderer => spriteRenderer;

        public static KombatEffect Play(
            string folder,
            Vector3 position,
            float framesPerSecond = 15f,
            int facing = 1,
            float scale = 1f,
            Color? tint = null,
            bool loop = false,
            float lifetime = 0f,
            int sortingOrder = 20)
        {
            Sprite[] sprites = KombatResources.Frames(folder);
            return Play(sprites, position, framesPerSecond, facing, scale, tint, loop, lifetime, sortingOrder);
        }

        public static KombatEffect Play(
            Sprite[] sprites,
            Vector3 position,
            float framesPerSecond = 15f,
            int facing = 1,
            float scale = 1f,
            Color? tint = null,
            bool loop = false,
            float lifetime = 0f,
            int sortingOrder = 20)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return null;
            }

            GameObject go = new GameObject("FX");
            go.transform.position = position;
            go.transform.localScale = new Vector3(scale * facing, scale, 1f);
            KombatEffect effect = go.AddComponent<KombatEffect>();
            effect.spriteRenderer = go.AddComponent<SpriteRenderer>();
            effect.spriteRenderer.sortingOrder = sortingOrder;
            effect.frames = sprites;
            effect.fps = framesPerSecond;
            effect.loop = loop;
            effect.lifetime = lifetime;
            effect.baseColor = tint ?? Color.white;
            effect.spriteRenderer.color = effect.baseColor;
            effect.spriteRenderer.sprite = sprites[0];
            return effect;
        }

        /// <summary>Faz o efeito se mover como partícula (sangue, ossos, gelo).</summary>
        public KombatEffect WithPhysics(Vector3 startVelocity, float gravityForce, float spinSpeed = 0f, bool bounceOnFloor = true)
        {
            velocity = startVelocity;
            gravity = gravityForce;
            spin = spinSpeed;
            bounce = bounceOnFloor;
            return this;
        }

        public KombatEffect WithFade(float seconds)
        {
            fadeOut = seconds;
            return this;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            age += dt;

            if (frames.Length > 1)
            {
                timer += dt * fps;
                int index = Mathf.FloorToInt(timer);
                if (index >= frames.Length)
                {
                    if (loop)
                    {
                        index %= frames.Length;
                    }
                    else if (lifetime <= 0f)
                    {
                        Destroy(gameObject);
                        return;
                    }
                    else
                    {
                        index = frames.Length - 1;
                    }
                }

                spriteRenderer.sprite = frames[index];
            }
            else if (lifetime <= 0f && !loop)
            {
                lifetime = 0.4f;
            }

            if (gravity != 0f || velocity != Vector3.zero)
            {
                velocity.y -= gravity * dt;
                Vector3 position = transform.position + velocity * dt;
                if (bounce && position.y < 0.02f && velocity.y < 0f)
                {
                    position.y = 0.02f;
                    velocity.y = -velocity.y * 0.25f;
                    velocity.x *= 0.5f;
                    spin *= 0.5f;
                }

                transform.position = position;
                transform.Rotate(0f, 0f, spin * dt);
            }

            if (lifetime > 0f)
            {
                if (fadeOut > 0f && age > lifetime - fadeOut)
                {
                    Color color = baseColor;
                    color.a *= Mathf.Clamp01((lifetime - age) / fadeOut);
                    spriteRenderer.color = color;
                }

                if (age >= lifetime)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
