using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    interface IDamagable
    {
        void Damage(float damage, ulong ownerId, int tickDiff) => Damage(damage);
        void Damage(float damage) { }
    }
}