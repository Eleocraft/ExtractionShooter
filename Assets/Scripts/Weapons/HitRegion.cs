using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class HitRegion : MonoBehaviour, IDamagable
    {
        [SerializeField] private PlayerLife parentDamagable;
        [SerializeField] private float Multiplier;
        public void OnHit(float damage)
        {
            parentDamagable.OnHit(damage * Multiplier);
        }
    }
}