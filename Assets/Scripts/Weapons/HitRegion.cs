using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class HitRegion : MonoBehaviour, IDamagable
    {
        [SerializeField] private PlayerLife ParentDamagable;
        [SerializeField] private float Multiplier;
        [SerializeField] private GameObject HitParticle;
        public void OnHit(float damage, Vector3 point, ulong ownerId)
        {
            if (ownerId == ParentDamagable.OwnerClientId)
                return;

            Instantiate(HitParticle, point, Quaternion.identity);
            ParentDamagable.OnHit(damage * Multiplier, point, ownerId);
        }
        public bool CanHit(ulong ownerId) => ParentDamagable.CanHit(ownerId);
    }
}