using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Projectile", menuName = "CustomObjects/Weapons/Projectile")]
    public class ProjectileInfo : ScriptableObject
    {
        [Header("Damage")]
        public float DefaultDamage;
        public float HeadshotDamage;
        [Header("Physics")]
        public float MuzzleVelocity;
        public float Drag;
        public float Dropoff;
        [Range(0, 0.3f)] public float PenetrationForce;
        [Header("DespawnChecks")]
        public float MaxDistance;
        public float MinVelocity;
        [Header("Graphics")]
        public GameObject Prefab;
        public HitMarker HitMarker;
        public HitMarker PenetrateMarker;
        public HitMarker ExitMarker;
    }
}