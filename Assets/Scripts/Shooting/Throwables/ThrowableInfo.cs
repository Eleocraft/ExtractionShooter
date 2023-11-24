using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Throwable", menuName = "CustomObjects/Utility/Throwable")]
    public class ThrowableInfo : ScriptableObject
    {
        public ThrowableExplosion Explosion;

        [Header("Physics")]
        public float Bouncyness;
        public float Drag;
        public float Dropoff;
        [Header("DespawnChecks")]
        public float MaxDistance;
        public float MinVelocity;
        [Header("Graphics")]
        public ProjectileGraphic Prefab;
    }
}
