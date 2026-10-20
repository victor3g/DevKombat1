using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>Música, locutor e efeitos sonoros. Sons que não existirem são ignorados.</summary>
    public class KombatAudio : MonoBehaviour
    {
        private static KombatAudio instance;

        private AudioSource music;
        private AudioSource announcer;
        private AudioSource effects;
        private string currentMusic;

        public static float MusicVolume = 0.45f;
        public static float EffectsVolume = 0.9f;

        private static KombatAudio Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("KombatAudio");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<KombatAudio>();
                    instance.music = go.AddComponent<AudioSource>();
                    instance.music.loop = true;
                    instance.music.playOnAwake = false;
                    instance.announcer = go.AddComponent<AudioSource>();
                    instance.announcer.playOnAwake = false;
                    instance.effects = go.AddComponent<AudioSource>();
                    instance.effects.playOnAwake = false;
                }

                return instance;
            }
        }

        public static void Music(string path)
        {
            KombatAudio audio = Instance;
            if (audio.currentMusic == path && audio.music.isPlaying)
            {
                return;
            }

            AudioClip clip = KombatResources.Clip(path);
            audio.currentMusic = path;
            audio.music.Stop();
            if (clip != null)
            {
                audio.music.clip = clip;
                audio.music.volume = MusicVolume;
                audio.music.Play();
            }
        }

        public static void RandomStageMusic()
        {
            AudioClip[] clips = KombatResources.Clips("MusicStages");
            if (clips.Length == 0)
            {
                Music("Music/fight_1");
                return;
            }

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            KombatAudio audio = Instance;
            audio.currentMusic = clip.name;
            audio.music.clip = clip;
            audio.music.volume = MusicVolume;
            audio.music.Play();
        }

        public static void StopMusic()
        {
            Instance.music.Stop();
            Instance.currentMusic = null;
        }

        /// <summary>Fala do locutor (Shao Kahn). Interrompe a fala anterior.</summary>
        public static void Announce(string path)
        {
            Announce(KombatResources.Clip(path));
        }

        public static void Announce(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            KombatAudio audio = Instance;
            audio.announcer.Stop();
            audio.announcer.clip = clip;
            audio.announcer.volume = 1f;
            audio.announcer.Play();
        }

        public static void Sfx(string path, float volume = 1f)
        {
            Sfx(KombatResources.Clip(path), volume);
        }

        public static void Sfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null)
            {
                return;
            }

            KombatAudio audio = Instance;
            audio.effects.pitch = pitch;
            audio.effects.PlayOneShot(clip, volume * EffectsVolume);
        }

        public static void RandomSfx(string folder, float volume = 1f, float pitch = 1f)
        {
            Sfx(KombatResources.RandomClip(folder), volume, pitch);
        }
    }
}
