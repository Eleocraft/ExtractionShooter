using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class BulletBarrier : MonoBehaviour, IDamagable
    {
        public void OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ulong ownerId, ref Vector3 velocity)
        {
            Instantiate(info.HitMarker, point, Quaternion.identity, transform).Initialize(normal, velocity);
            velocity = Vector3.zero;
        }
    }
}