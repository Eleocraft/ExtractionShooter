
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class BulletHole : HitMarker
    {
        [SerializeField] private float DecayTime;
        [SerializeField] private float FadeTime;
        public override void Initialize(Vector3 normal, Vector3 velocity)
        {
            transform.forward = -normal;
            transform.GetChild(0).localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        }
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
