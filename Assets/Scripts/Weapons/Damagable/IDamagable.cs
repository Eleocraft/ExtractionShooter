using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    interface IDamagable
    {
        bool OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ulong ownerId, int tickDiff, ref Vector3 velocity)
            => OnHit(info, point, normal, ref velocity);
        bool OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ref Vector3 velocity)
            => throw new System.Exception("No OnHit function overwritten");
        void OnExit(ProjectileInfo info, Vector3 point, Vector3 normal, Vector3 velocity) { }
    }
}