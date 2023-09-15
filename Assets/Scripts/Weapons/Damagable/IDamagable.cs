using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    interface IDamagable
    {
        bool OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ulong ownerId, int tickDiff, ref Vector3 velocity)
            => OnHit(info, point, normal, ref velocity);
        bool OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ref Vector3 velocity) => false;

        void ExplosionDamage(float damage, ulong ownerId, int tickDiff) => ExplosionDamage(damage);
        protected void ExplosionDamage(float damage) { }
        void OnExit(ProjectileInfo info, Vector3 point, Vector3 normal, Vector3 velocity) { }
    }
}