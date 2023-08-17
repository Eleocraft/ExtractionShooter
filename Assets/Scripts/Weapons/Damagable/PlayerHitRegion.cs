using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class HitRegion : MonoBehaviour, IDamagable
    {
        [SerializeField] private PlayerLife Player;
        [SerializeField] private float Multiplier;
        [SerializeField] private GameObject HitParticle;
        public void OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ulong ownerId, ref Vector3 velocity)
        {
            if (ownerId == Player.OwnerClientId)
                return;

            Instantiate(HitParticle, point, Quaternion.identity);
            Player.OnHit(info.Damage * Multiplier);
        }
    }
}