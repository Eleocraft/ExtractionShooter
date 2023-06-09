using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    interface IDamagable
    {
        public void OnHit(float damage, Vector3 point);
    }
}