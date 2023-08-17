using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    interface IDamagable
    {
        void OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ulong ownerId, ref Vector3 velocity);
        void OnExit(ProjectileInfo info, Vector3 point, Vector3 normal, Vector3 velocity) { }
    }
}