using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class HitRegion : MonoBehaviour, IDamagable
    {
        [SerializeField] private PlayerLife parentDamagable;
        [SerializeField] private float Multiplier;
        [SerializeField] private GameObject HitParticle;
        public void OnHit(float damage, Vector3 point)
        {
            Instantiate(HitParticle, point, Quaternion.identity);
            parentDamagable.OnHit(damage * Multiplier, point);
        }
    }
}