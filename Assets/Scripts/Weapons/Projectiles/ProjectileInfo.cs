using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Projectile", menuName = "CustomObjects/Weapons/Projectile")]
    public class ProjectileInfo : ScriptableObject
    {
        public float MuzzleVelocity;
        public float MinVelocity;
        public float Drag;
        public float Dropoff;
        public float Damage;
        public float MaxDistance;
        [Range(0, 0.3f)] public float PenetrationForce;
        public GameObject Prefab;
        public HitMarker HitMarker;
        public HitMarker PenetrateMarker;
        public HitMarker ExitMarker;
    }
}