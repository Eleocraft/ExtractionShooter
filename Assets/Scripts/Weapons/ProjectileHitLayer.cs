using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ProjectileHitLayer : MonoSingleton<ProjectileHitLayer>
    {
        [SerializeField] private LayerMask canHitFriendly;
        [SerializeField] private LayerMask canHitFromEnemy;
        [SerializeField] private LayerMask penetrable;

        public static LayerMask CanHitFriendly => Instance.canHitFriendly;
        public static LayerMask CanHitFromEnemy => Instance.canHitFromEnemy;
        public static LayerMask Penetrable => Instance.penetrable;
    }
}

