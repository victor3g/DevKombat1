using System.Collections.Generic;
using UnityEngine;

namespace DevKombat.Fighters
{
    /// <summary>
    /// Componente base do lutador. Aplica a configuração selecionada e mantém
    /// os pontos de extensão para movimento, ataques e hitboxes.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(AudioSource))]
    public class FighterController : MonoBehaviour
    {
        [Header("Configuração")]
        [Tooltip("Dados usados para inicializar o lutador ao entrar na cena.")]
        [SerializeField] private FighterData fighterData;
        [SerializeField] private bool initializeOnAwake = true;

        private Animator fighterAnimator;
        private AudioSource fighterAudioSource;

        public FighterData Data { get; private set; }
        public float MaxHealth => Data != null ? Data.MaxHealth : 0f;
        public float CurrentHealth { get; private set; }
        public float MoveSpeed { get; private set; }
        public float JumpForce { get; private set; }
        public float PunchDamage { get; private set; }
        public float KickDamage { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsDefeated => IsInitialized && CurrentHealth <= 0f;

        private void Awake()
        {
            CacheComponents();

            if (initializeOnAwake && fighterData != null)
            {
                InitializeFighter(fighterData);
            }
        }

        /// <summary>
        /// Carrega estatísticas, animações e referências de áudio do personagem.
        /// Pode ser chamado novamente para trocar o lutador no mesmo GameObject.
        /// </summary>
        public void InitializeFighter(FighterData data)
        {
            if (data == null)
            {
                Debug.LogError("Não é possível inicializar um lutador sem FighterData.", this);
                return;
            }

            CacheComponents();

            fighterData = data;
            Data = data;
            CurrentHealth = data.MaxHealth;
            MoveSpeed = data.MoveSpeed;
            JumpForce = data.JumpForce;
            PunchDamage = data.PunchDamage;
            KickDamage = data.KickDamage;

            fighterAnimator.runtimeAnimatorController = data.AnimatorController;
            IsInitialized = true;
        }

        /// <summary>
        /// Reproduz aleatoriamente uma fala de entrada configurada no FighterData.
        /// </summary>
        public void PlayIntroAudio()
        {
            PlayRandomClip(Data != null ? Data.IntroClips : null);
        }

        /// <summary>
        /// Reproduz aleatoriamente um som de ataque configurado no FighterData.
        /// </summary>
        public void PlayAttackAudio()
        {
            PlayRandomClip(Data != null ? Data.AttackClips : null);
        }

        /// <summary>
        /// Reproduz aleatoriamente um som de dano configurado no FighterData.
        /// </summary>
        public void PlayHitAudio()
        {
            PlayRandomClip(Data != null ? Data.HitClips : null);
        }

        /// <summary>
        /// Reproduz aleatoriamente uma frase de vitória configurada no FighterData.
        /// </summary>
        public void PlayVictoryAudio()
        {
            PlayRandomClip(Data != null ? Data.VictoryClips : null);
        }

        /// <summary>
        /// Ponto inicial para o futuro sistema de dano e reação de hitbox.
        /// </summary>
        public virtual void TakeDamage(float damage)
        {
            if (!IsInitialized || damage <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            PlayHitAudio();

            // TODO: disparar animação de Hit e avisar o sistema de rounds ao chegar a zero.
        }

        private void CacheComponents()
        {
            if (fighterAnimator == null)
            {
                fighterAnimator = GetComponent<Animator>();
            }

            if (fighterAudioSource == null)
            {
                fighterAudioSource = GetComponent<AudioSource>();
                fighterAudioSource.playOnAwake = false;
            }
        }

        private void PlayRandomClip(IReadOnlyList<AudioClip> clips)
        {
            if (clips == null || clips.Count == 0)
            {
                return;
            }

            AudioClip clip = clips[Random.Range(0, clips.Count)];
            if (clip != null)
            {
                fighterAudioSource.PlayOneShot(clip);
            }
        }

        // TODO: adicionar leitura de input e movimentação usando Rigidbody2D.
        // TODO: adicionar hitboxes/hurtboxes e janelas de dano por Animation Events.
    }
}
