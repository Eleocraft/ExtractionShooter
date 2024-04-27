using System.Collections.Generic;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public enum DefaultSoundEffect { MenuClick, MenuClickLight, PlayerHit, PlayerHeadshot }
    public class SFXSource : MonoBehaviour
    {
        [SerializeField] private EnumDictionary<DefaultSoundEffect, AudioClip> SoundEffects;
        private static AudioSource _source;
        private static Dictionary<DefaultSoundEffect, AudioClip> _soundEffects;
        private void OnValidate() {
            SoundEffects.Update();
        }
        private void Awake()
        {
            _soundEffects = SoundEffects.GetDictionary();
            _source = GetComponent<AudioSource>();
        }
        public static void PlaySoundEffect(AudioClip clip) => _source.PlayOneShot(clip);
        public static void PlaySoundEffect(DefaultSoundEffect effect) => _source.PlayOneShot(_soundEffects[effect]);
    }
}
