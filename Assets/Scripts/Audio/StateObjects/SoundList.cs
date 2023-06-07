using UnityEngine;

namespace ExoplanetStudios
{
    [CreateAssetMenu(fileName = "New Music List", menuName = "CustomObjects/Audio/MusicList")]
    public class SoundList : SoundStateObject
    {
        [MinMaxRange(0, 500)] public RangeSlider clipTimer;
        [SerializeField] private AudioClip[] music;
        private int lastMusicID;
        public override AudioClip GetClip()
        {

            int musicID;
            do
                musicID = Random.Range(0, music.Length);
            while (musicID == lastMusicID && music.Length > 1);
            lastMusicID = musicID;
            return music[musicID];
        }
        public override float GetPauseTime()
        {
            return clipTimer.RandomValue();
        }
        public override bool StateChanged() => false;
    }
}