using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>Atalhos para os efeitos clássicos: sangue, ossos, faíscas e fantasmas.</summary>
    public static class KombatFX
    {
        public static bool BloodEnabled = true;

        public static void Blood(Vector2 position, int direction, float amount)
        {
            if (!BloodEnabled)
            {
                return;
            }

            KombatEffect.Play("FX/blood_spurt", position, 24f, direction, 1.3f, sortingOrder: 30);
            int drops = Mathf.Clamp(Mathf.RoundToInt(amount * 0.8f), 2, 14);
            Sprite[] dropFrames = KombatResources.Frames("FX/blood_drops");
            if (dropFrames.Length == 0)
            {
                return;
            }

            for (int i = 0; i < drops; i++)
            {
                Sprite sprite = dropFrames[Random.Range(0, dropFrames.Length)];
                Vector3 velocity = new Vector3(direction * Random.Range(1.5f, 6f), Random.Range(2f, 7f), 0f);
                KombatEffect.Play(new[] { sprite }, position, 0f, direction, 1f, lifetime: Random.Range(0.8f, 1.6f), sortingOrder: 30)
                    ?.WithPhysics(velocity, 22f, Random.Range(-90f, 90f))
                    .WithFade(0.3f);
            }
        }

        public static void BloodBurst(Vector2 position, int direction)
        {
            if (!BloodEnabled)
            {
                return;
            }

            KombatEffect.Play("FX/blood_burst", position, 18f, direction, 1.4f, sortingOrder: 30);
            Blood(position, direction, 16f);
            Blood(position, -direction, 10f);
        }

        public static void Gibs(Vector2 position, int count, float force = 9f)
        {
            Sprite[] pieces = KombatResources.Frames("FX/gibs");
            if (pieces.Length == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Sprite sprite = pieces[Random.Range(0, pieces.Length)];
                Vector3 velocity = new Vector3(Random.Range(-force, force) * 0.6f, Random.Range(force * 0.5f, force * 1.3f), 0f);
                Vector2 start = position + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.5f, 2.8f));
                KombatEffect.Play(new[] { sprite }, start, 0f, 1, 1.2f, lifetime: Random.Range(2.5f, 4f), sortingOrder: 25)
                    ?.WithPhysics(velocity, 24f, Random.Range(-540f, 540f))
                    .WithFade(0.8f);
            }

            if (BloodEnabled)
            {
                for (int i = 0; i < 4; i++)
                {
                    Blood(position + Vector2.up * Random.Range(0.6f, 2.4f), Random.value > 0.5f ? 1 : -1, 10f);
                }
            }
        }

        public static void IceShards(Vector2 position, int count)
        {
            KombatEffect.Play("FX/ice_break", position, 18f, 1, 1.4f, sortingOrder: 26);
            Sprite[] shards = KombatResources.Frames("FX/ice_blast");
            if (shards.Length == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Sprite sprite = shards[Random.Range(0, shards.Length)];
                Vector3 velocity = new Vector3(Random.Range(-6f, 6f), Random.Range(4f, 11f), 0f);
                KombatEffect.Play(new[] { sprite }, position + Vector2.up * Random.Range(0.4f, 2.8f), 0f, 1, 0.6f,
                        lifetime: Random.Range(1.5f, 2.5f), sortingOrder: 26)
                    ?.WithPhysics(velocity, 22f, Random.Range(-360f, 360f))
                    .WithFade(0.6f);
            }
        }

        /// <summary>Cópia translúcida do lutador, usada nos rastros de investidas.</summary>
        public static void Afterimage(SpriteRenderer source, Color color)
        {
            if (source == null || source.sprite == null)
            {
                return;
            }

            KombatEffect ghost = KombatEffect.Play(new[] { source.sprite }, source.transform.position, 0f, 1, 1f,
                color, lifetime: 0.25f, sortingOrder: source.sortingOrder - 1);
            if (ghost != null)
            {
                ghost.transform.localScale = source.transform.lossyScale;
                ghost.WithFade(0.25f);
            }
        }
    }
}
