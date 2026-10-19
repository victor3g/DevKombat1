using UnityEngine;

namespace DevKombat.Fighters
{
    /// <summary>
    /// IA leve para o primeiro teste contra o computador.
    /// Aproxima-se do alvo, recua quando está perto demais e ataca ao alcance.
    /// </summary>
    [RequireComponent(typeof(PrototypeFighter2D))]
    public class PrototypeFighterAI : MonoBehaviour
    {
        [Header("Comportamento")]
        [SerializeField] private float preferredDistance = 0.9f;
        [SerializeField] private float retreatDistance = 0.55f;
        [SerializeField] private float attackDistance = 1.15f;
        [SerializeField] private Vector2 decisionDelayRange = new Vector2(0.35f, 0.7f);
        [Range(0f, 1f)] [SerializeField] private float kickChance = 0.4f;
        [Range(0f, 1f)] [SerializeField] private float jumpChance = 0.08f;

        private PrototypeFighter2D motor;
        private FighterController fighter;
        private Transform target;
        private FighterController targetFighter;
        private float nextDecisionTime;

        public void Initialize(Transform targetTransform)
        {
            target = targetTransform;
            targetFighter = target != null ? target.GetComponent<FighterController>() : null;
        }

        private void Awake()
        {
            motor = GetComponent<PrototypeFighter2D>();
            fighter = GetComponent<FighterController>();
            motor.SetPlayerInputEnabled(false);
        }

        private void Update()
        {
            if (target == null || fighter == null || fighter.IsDefeated ||
                targetFighter == null || targetFighter.IsDefeated)
            {
                motor.SetMovementInput(0f);
                return;
            }

            float horizontalDistance = target.position.x - transform.position.x;
            float absoluteDistance = Mathf.Abs(horizontalDistance);
            float directionToTarget = Mathf.Sign(horizontalDistance);
            motor.FaceRight(horizontalDistance >= 0f);

            if (!motor.CanAct)
            {
                motor.SetMovementInput(0f);
                return;
            }

            if (absoluteDistance > preferredDistance)
            {
                motor.SetMovementInput(directionToTarget);
            }
            else if (absoluteDistance < retreatDistance)
            {
                motor.SetMovementInput(-directionToTarget * 0.65f);
            }
            else
            {
                motor.SetMovementInput(0f);
            }

            if (Time.time < nextDecisionTime)
            {
                return;
            }

            nextDecisionTime = Time.time + Random.Range(decisionDelayRange.x, decisionDelayRange.y);

            if (absoluteDistance <= attackDistance)
            {
                if (Random.value < kickChance)
                {
                    motor.TryKick();
                }
                else
                {
                    motor.TryPunch();
                }
            }
            else if (Random.value < jumpChance)
            {
                motor.TryJump();
            }
        }
    }
}
