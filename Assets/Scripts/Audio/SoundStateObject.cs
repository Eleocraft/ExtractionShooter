using UnityEngine;

namespace ExoplanetStudios
{
    public abstract class SoundStateObject : ScriptableObject
    {
        public abstract bool StateChanged();
        public abstract float GetPauseTime();
        public abstract AudioClip GetClip();
    }
}