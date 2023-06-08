using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Projectile", menuName = "CustomObjects/Weapons/Projectile")]
    public class ProjectileInfo : ScriptableObject
    {
        public float MuzzleVelocity;
        public float Drag;
        public float Dropoff;
        public float Damage;
        public float LifeTime;
        public GameObject Prefab;
        public GameObject HitMarker;
    }
}