using System.Collections;
using UnityEngine;

namespace DevKombat.Fighters
{
    /// <summary>
    /// Controlador completo para o primeiro protótipo jogável.
    /// Basta arrastar para cada lista os sprites gerados por IA, na ordem dos frames.
    /// Uma lista com apenas uma imagem também funciona.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class PrototypeFighter2D : MonoBehaviour
    {
        private enum AnimationState
        {
            Idle,
            Walk,
            Jump,
            Punch,
            Kick,
            Hit,
            Defeat,
            Victory
        }

        [Header("Sprites gerados por IA")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] jumpFrames;
        [SerializeField] private Sprite[] punchFrames;
        [SerializeField] private Sprite[] kickFrames;
        [SerializeField] private Sprite[] hitFrames;
        [SerializeField] private Sprite[] defeatFrames;
        [SerializeField] private Sprite[] victoryFrames;
        [Min(1f)] [SerializeField] private float framesPerSecond = 10f;

        [Header("Controles")]
        [SerializeField] private KeyCode moveLeft = KeyCode.A;
        [SerializeField] private KeyCode moveRight = KeyCode.D;
        [SerializeField] private KeyCode jump = KeyCode.W;
        [SerializeField] private KeyCode punch = KeyCode.F;
        [SerializeField] private KeyCode kick = KeyCode.G;

        [Header("Chão")]
        [SerializeField] private LayerMask groundLayers = -1;
        [Min(0.01f)] [SerializeField] private float groundCheckDistance = 0.12f;

        [Header("Ataques")]
        [SerializeField] private LayerMask targetLayers = -1;
        [SerializeField] private Vector2 attackOffset = new Vector2(0.8f, 0.1f);
        [SerializeField] private Vector2 attackSize = new Vector2(0.9f, 1.2f);
        [Min(0.05f)] [SerializeField] private float punchDuration = 0.25f;
        [Min(0.05f)] [SerializeField] private float kickDuration = 0.35f;
        [Min(0f)] [SerializeField] private float attackCooldown = 0.15f;

        private FighterController fighter;
        private SpriteRenderer spriteRenderer;
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private AnimationState currentState;
        private float animationTimer;
        private int currentFrame;
        private float nextAttackTime;
        private bool actionLocked;
        private bool defeated;
        private bool facingRight = true;

        public void ConfigureControls(
            KeyCode left,
            KeyCode right,
            KeyCode jumpKey,
            KeyCode punchKey,
            KeyCode kickKey)
        {
            moveLeft = left;
            moveRight = right;
            jump = jumpKey;
            punch = punchKey;
            kick = kickKey;
        }

        private void Awake()
        {
            fighter = GetComponent<FighterController>();
            EnsurePrototypeComponents();
            PlayAnimation(AnimationState.Idle, true);
        }

        private void Update()
        {
            UpdateFrameAnimation();

            if (defeated || !fighter.IsInitialized)
            {
                return;
            }

            if (!actionLocked)
            {
                ReadActions();
            }
        }

        private void FixedUpdate()
        {
            if (defeated || actionLocked || !fighter.IsInitialized)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }

            float direction = 0f;
            if (Input.GetKey(moveLeft)) direction -= 1f;
            if (Input.GetKey(moveRight)) direction += 1f;

            body.linearVelocity = new Vector2(direction * fighter.MoveSpeed, body.linearVelocity.y);

            if (direction != 0f)
            {
                facingRight = direction > 0f;
                spriteRenderer.flipX = !facingRight;
                PlayAnimation(AnimationState.Walk);
            }
            else if (IsGrounded())
            {
                PlayAnimation(AnimationState.Idle);
            }
            else
            {
                PlayAnimation(AnimationState.Jump);
            }
        }

        private void ReadActions()
        {
            if (Input.GetKeyDown(jump) && IsGrounded())
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, fighter.JumpForce);
                PlayAnimation(AnimationState.Jump, true);
            }

            if (Time.time < nextAttackTime)
            {
                return;
            }

            if (Input.GetKeyDown(punch))
            {
                StartCoroutine(Attack(AnimationState.Punch, fighter.PunchDamage, punchDuration));
            }
            else if (Input.GetKeyDown(kick))
            {
                StartCoroutine(Attack(AnimationState.Kick, fighter.KickDamage, kickDuration));
            }
        }

        private IEnumerator Attack(AnimationState state, float damage, float duration)
        {
            actionLocked = true;
            nextAttackTime = Time.time + duration + attackCooldown;
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            PlayAnimation(state, true);
            fighter.PlayAttackAudio();

            // A hitbox aparece no meio do golpe.
            yield return new WaitForSeconds(duration * 0.5f);
            ApplyAttackDamage(damage);
            yield return new WaitForSeconds(duration * 0.5f);

            actionLocked = false;
            PlayAnimation(IsGrounded() ? AnimationState.Idle : AnimationState.Jump, true);
        }

        private void ApplyAttackDamage(float damage)
        {
            Vector2 direction = facingRight ? Vector2.right : Vector2.left;
            Vector2 center = (Vector2)transform.position + new Vector2(
                attackOffset.x * direction.x,
                attackOffset.y);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackSize, 0f, targetLayers);
            foreach (Collider2D hit in hits)
            {
                FighterController target = hit.GetComponentInParent<FighterController>();
                if (target == null || target == fighter)
                {
                    continue;
                }

                target.TakeDamage(damage);
                PrototypeFighter2D targetPrototype = target.GetComponent<PrototypeFighter2D>();
                if (targetPrototype != null)
                {
                    targetPrototype.ReactToDamage();
                }

                if (target.CurrentHealth <= 0f)
                {
                    CelebrateVictory();
                }

                break;
            }
        }

        public void ReactToDamage()
        {
            StopAllCoroutines();
            actionLocked = true;

            if (fighter.CurrentHealth <= 0f)
            {
                defeated = true;
                body.linearVelocity = Vector2.zero;
                PlayAnimation(AnimationState.Defeat, true);
                return;
            }

            PlayAnimation(AnimationState.Hit, true);
            StartCoroutine(ReleaseAfterHit(0.25f));
        }

        public void CelebrateVictory()
        {
            actionLocked = true;
            body.linearVelocity = Vector2.zero;
            PlayAnimation(AnimationState.Victory, true);
            fighter.PlayVictoryAudio();
        }

        public void FaceRight(bool value)
        {
            facingRight = value;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !facingRight;
            }
        }

        private IEnumerator ReleaseAfterHit(float delay)
        {
            yield return new WaitForSeconds(delay);
            actionLocked = false;
        }

        private bool IsGrounded()
        {
            Bounds bounds = bodyCollider.bounds;
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y - 0.02f);
            return Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayers);
        }

        private void EnsurePrototypeComponents()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            body = GetComponent<Rigidbody2D>();
            if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            bodyCollider = GetComponent<Collider2D>();
            if (bodyCollider == null)
            {
                CapsuleCollider2D capsule = gameObject.AddComponent<CapsuleCollider2D>();
                capsule.size = new Vector2(0.8f, 1.8f);
                bodyCollider = capsule;
            }
        }

        private void PlayAnimation(AnimationState state, bool restart = false)
        {
            if (!restart && currentState == state)
            {
                return;
            }

            currentState = state;
            currentFrame = 0;
            animationTimer = 0f;
            ApplyCurrentFrame();
        }

        private void UpdateFrameAnimation()
        {
            Sprite[] frames = GetFrames(currentState);
            if (frames == null || frames.Length <= 1)
            {
                return;
            }

            animationTimer += Time.deltaTime;
            float frameDuration = 1f / framesPerSecond;
            if (animationTimer < frameDuration)
            {
                return;
            }

            animationTimer -= frameDuration;
            currentFrame = (currentFrame + 1) % frames.Length;
            ApplyCurrentFrame();
        }

        private void ApplyCurrentFrame()
        {
            Sprite[] frames = GetFrames(currentState);
            if (frames != null && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[Mathf.Clamp(currentFrame, 0, frames.Length - 1)];
            }
        }

        private Sprite[] GetFrames(AnimationState state)
        {
            switch (state)
            {
                case AnimationState.Walk: return HasFrames(walkFrames) ? walkFrames : idleFrames;
                case AnimationState.Jump: return HasFrames(jumpFrames) ? jumpFrames : idleFrames;
                case AnimationState.Punch: return HasFrames(punchFrames) ? punchFrames : idleFrames;
                case AnimationState.Kick: return HasFrames(kickFrames) ? kickFrames : idleFrames;
                case AnimationState.Hit: return HasFrames(hitFrames) ? hitFrames : idleFrames;
                case AnimationState.Defeat: return HasFrames(defeatFrames) ? defeatFrames : idleFrames;
                case AnimationState.Victory: return HasFrames(victoryFrames) ? victoryFrames : idleFrames;
                default: return idleFrames;
            }
        }

        private static bool HasFrames(Sprite[] frames)
        {
            return frames != null && frames.Length > 0;
        }

        private void OnDrawGizmosSelected()
        {
            bool right = !Application.isPlaying || facingRight;
            Vector2 center = (Vector2)transform.position + new Vector2(
                attackOffset.x * (right ? 1f : -1f),
                attackOffset.y);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
            Gizmos.DrawCube(center, attackSize);
        }
    }
}
