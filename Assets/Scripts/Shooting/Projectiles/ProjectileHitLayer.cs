using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ProjectileHitLayer : MonoSingleton<ProjectileHitLayer>
    {
        [SerializeField] private LayerMask canHit;
        [SerializeField] private LayerMask throwableHit;

        public static LayerMask CanHit => Instance.canHit;
        public static LayerMask TrowableHit => Instance.throwableHit;
    }
}

