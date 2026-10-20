using DevKombat.Fighters;
using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>
    /// Projétil de golpe especial (bola de fogo, gelo, lança, sai, leque...).
    /// Atualizado pelo <see cref="KombatMatch"/> a cada quadro fixo.
    /// </summary>
    public class KombatProjectile : MonoBehaviour
    {
        private KombatFighter owner;
        private AbilityPreset preset;
        private FighterSpecialMove move;
        private Sprite[] frames;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer ropeRenderer;
        private Vector2 position;
        private Vector2 velocity;
        private float damage;
        private float travelled;
        private bool returning;
        private float animTime;
        private bool removed;
        private int pullFrames;
        private Vector2 halfSize = new Vector2(0.45f, 0.35f);

        public KombatFighter Owner => owner;
        public bool IsAlive => !removed;
        public Vector2 Position => position;
        public int Direction => velocity.x >= 0f ? 1 : -1;

        public static KombatProjectile Spawn(KombatFighter owner, AbilityPreset preset, FighterSpecialMove move, Sprite[] frames, float damage)
        {
            GameObject go = new GameObject("Projectile_" + preset.Ability);
            KombatProjectile projectile = go.AddComponent<KombatProjectile>();
            projectile.owner = owner;
            projectile.preset = preset;
            projectile.move = move;
            projectile.frames = frames;
            projectile.damage = damage;
            projectile.position = owner.Position + new Vector2(owner.Facing * 0.9f, preset.Height);
            projectile.velocity = new Vector2(owner.Facing * preset.Speed, 0f);
            projectile.spriteRenderer = go.AddComponent<SpriteRenderer>();
            projectile.spriteRenderer.sortingOrder = 18;
            projectile.spriteRenderer.color = move.EffectTint;
            if (frames != null && frames.Length > 0)
            {
                projectile.spriteRenderer.sprite = frames[0];
                Bounds bounds = frames[frames.Length / 2].bounds;
                projectile.halfSize = new Vector2(Mathf.Max(0.3f, bounds.extents.x * preset.EffectScale),
                    Mathf.Max(0.25f, bounds.extents.y * preset.EffectScale));
            }
            else
            {
                projectile.spriteRenderer.sprite = KombatResources.WhiteSprite;
                projectile.spriteRenderer.color = move.EffectTint == Color.white ? new Color(1f, 0.6f, 0.1f) : move.EffectTint;
                go.transform.localScale = new Vector3(0.6f, 0.4f, 1f);
            }

            if (preset.Ability == MKAbility.Spear)
            {
                Sprite[] rope = KombatResources.Frames("FX/spear_rope");
                if (rope.Length > 0)
                {
                    projectile.ropeRenderer = new GameObject("Rope").AddComponent<SpriteRenderer>();
                    projectile.ropeRenderer.sprite = rope[0];
                    projectile.ropeRenderer.sortingOrder = 17;
                }
            }

            owner.Match?.RegisterProjectile(projectile);
            projectile.Render();
            return projectile;
        }

        public KombatBox Box => new KombatBox(position.x - halfSize.x, position.y - halfSize.y,
            position.x + halfSize.x, position.y + halfSize.y);

        public void Tick()
        {
            if (removed)
            {
                return;
            }

            if (pullFrames > 0)
            {
                pullFrames--;
                position = Vector2.Lerp(position, owner.Position + new Vector2(owner.Facing * 0.9f, preset.Height), 0.35f);
                if (pullFrames == 0)
                {
                    Remove();
                }

                return;
            }

            const float dt = 1f / 60f;
            position += velocity * dt;
            travelled += Mathf.Abs(velocity.x) * dt;

            if (preset.Returns && !returning && travelled > 6.5f)
            {
                returning = true;
                velocity.x = -velocity.x;
            }

            if (returning && Mathf.Abs(position.x - owner.Position.x) < 0.6f)
            {
                Remove();
                return;
            }

            KombatFighter target = owner.Opponent;
            if (target != null && target.CanBeHitBy(Box))
            {
                HitTarget(target);
                return;
            }

            if (owner.Match != null && !owner.Match.IsInsideView(position.x, 1.5f))
            {
                Remove();
            }
        }

        private void HitTarget(KombatFighter target)
        {
            bool landed = target.TakeHit(owner, new KombatHit
            {
                Damage = damage,
                Level = preset.Level,
                Reaction = preset.Reaction,
                Pushback = 1.6f,
                Hitstun = 20,
                Special = true,
                Point = position,
                ImpactFolder = preset.ImpactFolder,
                Sound = string.IsNullOrEmpty(preset.HitSound) ? null : KombatResources.Clip(preset.HitSound)
            });

            if (landed && preset.Reaction == HitReaction.Pull)
            {
                // A lança volta trazendo o oponente.
                pullFrames = 14;
                return;
            }

            if (!landed && string.IsNullOrEmpty(preset.ImpactFolder))
            {
                KombatEffect.Play("FX/shadow_impact", position, 20f, -Direction, 0.8f, sortingOrder: 35);
            }

            Remove();
        }

        /// <summary>Dois projéteis que se encontram se anulam, como no MK.</summary>
        public void Clash()
        {
            KombatEffect.Play(string.IsNullOrEmpty(preset.ImpactFolder) ? "FX/explosion" : preset.ImpactFolder,
                position, 20f, Direction, string.IsNullOrEmpty(preset.ImpactFolder) ? 0.6f : 1f, sortingOrder: 35);
            Remove();
        }

        public void Remove()
        {
            if (removed)
            {
                return;
            }

            removed = true;
            owner.OnProjectileFinished(this);
            if (ropeRenderer != null)
            {
                Destroy(ropeRenderer.gameObject);
            }

            Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (removed)
            {
                return;
            }

            if (frames != null && frames.Length > 1)
            {
                animTime += Time.deltaTime * preset.EffectFps;
                int index = Mathf.FloorToInt(animTime);
                // Projéteis repetem a parte final da animação (o corpo do disparo).
                int loopStart = Mathf.Min(frames.Length - 1, frames.Length / 3);
                if (index >= frames.Length)
                {
                    index = loopStart + (index - frames.Length) % Mathf.Max(1, frames.Length - loopStart);
                }

                spriteRenderer.sprite = frames[index];
            }

            Render();
        }

        private void Render()
        {
            transform.position = new Vector3(position.x, position.y, 0f);
            if (frames != null && frames.Length > 0)
            {
                float scale = preset.EffectScale;
                transform.localScale = new Vector3(scale * Direction, scale, 1f);
            }

            if (ropeRenderer != null)
            {
                Vector2 hand = owner.Position + new Vector2(owner.Facing * 0.6f, preset.Height);
                Vector2 middle = (hand + position) * 0.5f;
                float width = ropeRenderer.sprite.bounds.size.x;
                float length = Mathf.Abs(position.x - hand.x);
                ropeRenderer.transform.position = new Vector3(middle.x, middle.y, 0f);
                ropeRenderer.transform.localScale = new Vector3(width > 0f ? length / width : 1f, 1f, 1f);
            }
        }
    }
}
