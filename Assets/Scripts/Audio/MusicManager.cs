using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ExoplanetStudios
{
    public class MusicManager : MonoSingleton<MusicManager>
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private float fadeTime;
        [SerializeField] private float updateTime = 5f;
        private AudioSource audioSource;
        [SerializeField] private SoundStateObject musicStateObj;

        private float pauseTimer;
        private float updateTimer;

        const string musicVolumeName = "MusicVol";
        private float defaultVolume;

        private void Start()
        {
            mixer.GetFloat(musicVolumeName, out defaultVolume);
            audioSource = GetComponent<AudioSource>();
            pauseTimer = musicStateObj.GetPauseTime();
            updateTimer = updateTime;
        }
        private void UpdateMusicState() // start fading out the old music for change
        {
            StartCoroutine(FadeMixerGroup.StartFade(mixer, musicVolumeName, fadeTime, callback : UpdateMusic));
        }
        private void UpdateMusic() // Play a new music
        {
            mixer.SetFloat(musicVolumeName, defaultVolume);
            audioSource.clip = musicStateObj.GetClip();
            audioSource.Play();
            pauseTimer = musicStateObj.GetPauseTime();
        }
        private void Update()
        {
            if (!audioSource.isPlaying)
            {
                if (pauseTimer > 0)
                    pauseTimer -= Time.deltaTime;
                if (pauseTimer <= 0)
                    UpdateMusic();
            }
            // update timer to check if the music should be changed (every <updateTime> seconds)
            if (updateTimer > 0)
                updateTimer -= Time.deltaTime;
            if (updateTimer <= 0)
            {
                if (musicStateObj.StateChanged())
                    UpdateMusicState();
                updateTimer = updateTime;
            }
        }
        [Command]
        public static void RerollMusic(List<string> args)
        {
            Instance.UpdateMusicState();
        }
    }
}