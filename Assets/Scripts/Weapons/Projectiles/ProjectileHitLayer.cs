using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ProjectileHitLayer : MonoSingleton<ProjectileHitLayer>
    {
        [SerializeField] private LayerMask canHit;

        public static LayerMask CanHit => Instance.canHit;
    }
}

