using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class SFXSource : MonoBehaviour
    {
        public static AudioSource Source;
        void Awake()
        {
            Source = GetComponent<AudioSource>();
        }
    }
}
