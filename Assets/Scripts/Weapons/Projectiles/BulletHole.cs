
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class BulletHole : MonoBehaviour
    {
        [SerializeField] private float DecayTime;
        [SerializeField] private float FadeTime;
        void Start()
        {
            GetComponent<FadeController>().StartTimer(DecayTime, FadeTime, Destroy);
        }
        void Destroy()
        {
            Destroy(gameObject);
        }
    }
}
