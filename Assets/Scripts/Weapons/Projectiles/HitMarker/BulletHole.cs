
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class BulletHole : HitMarker
    {
        [SerializeField] private float DecayTime;
        [SerializeField] private float FadeTime;
        public override void Initialize(Vector3 normal, Vector3 velocity)
        {
            transform.localScale = new Vector3(1/transform.lossyScale.x, 1/transform.lossyScale.y, 1/transform.lossyScale.z);
            transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            transform.GetChild(0).localRotation = Quaternion.Euler(-90, Random.Range(0, 360), 0);
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
