using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ProjectileHitLayer : MonoSingleton<ProjectileHitLayer>
    {
        [SerializeField] private LayerMask canHit;
        [SerializeField] private LayerMask penetrable;

        public static LayerMask CanHit => Instance.canHit;
        public static LayerMask Penetrable => Instance.penetrable;
    }
}

