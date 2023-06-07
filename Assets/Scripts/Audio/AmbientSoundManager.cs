using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ExoplanetStudios
{
    public class AmbientSoundManager : MonoSingleton<AmbientSoundManager>
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private float fadeTime;
        [SerializeField] private float updateTime = 5f;
        private AudioSource audioSource;
        [SerializeField] private SoundStateObject ambientStateObj;

        private float pauseTimer;
        private float updateTimer;

        const string soundVolumeName = "AmbientVol";
        private float defaultVolume;

        private void Start()
        {
            mixer.GetFloat(soundVolumeName, out defaultVolume);
            audioSource = GetComponent<AudioSource>();
            pauseTimer = ambientStateObj.GetPauseTime();
            updateTimer = updateTime;
        }
        private void UpdateAmbientState() // start fading out the old ambient for change
        {
            StartCoroutine(FadeMixerGroup.StartFade(mixer, soundVolumeName, fadeTime, callback : UpdateAmbient));
        }
        private void UpdateAmbient() // Play a new ambient
        {
            mixer.SetFloat(soundVolumeName, defaultVolume);
            audioSource.clip = ambientStateObj.GetClip();
            audioSource.Play();
            pauseTimer = ambientStateObj.GetPauseTime();
        }
        private void Update()
        {
            if (!audioSource.isPlaying)
            {
                if (pauseTimer > 0)
                    pauseTimer -= Time.deltaTime;
                if (pauseTimer <= 0)
                    UpdateAmbient();
            }
            // update timer to check if the ambient should be changed (every <updateTime> seconds)
            if (updateTimer > 0)
                updateTimer -= Time.deltaTime;
            if (updateTimer <= 0)
            {
                if (ambientStateObj.StateChanged())
                    UpdateAmbientState();
                updateTimer = updateTime;
            }
        }
        [Command]
        public static void RerollAmbient(List<string> args)
        {
            Instance.UpdateAmbientState();
        }
    }
}