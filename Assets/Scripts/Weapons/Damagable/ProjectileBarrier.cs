using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ProjectileBarrier : MonoBehaviour, IDamagable
    {
        public void OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ref Vector3 velocity)
        {
            Instantiate(info.HitMarker, point, Quaternion.identity, transform).Initialize(normal, velocity);
            velocity = Vector3.zero;
        }
    }
}