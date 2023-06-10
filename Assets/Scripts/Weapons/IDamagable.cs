using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    interface IDamagable
    {
        void OnHit(float damage, Vector3 point, ulong ownerId);
        bool CanHit(ulong ownerId);
    }
}