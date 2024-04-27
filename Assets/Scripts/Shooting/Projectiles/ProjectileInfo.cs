using UnityEngine;


public enum DamageType { Default, Headshot }
namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Projectile", menuName = "CustomObjects/Weapons/Projectile")]
    public class ProjectileInfo : ScriptableObject
    {
        [Header("Damage + effects")]
        public EnumDictionary<DamageType, float> Damages;
        public float DamageDropoffVelocityPercent = 0.8f;
        public bool Stun;

        [Header("Physics")]
        public float MaxVelocity;
        public float Drag;
        public float Dropoff;
        [Range(0, 0.3f)] public float PenetrationForce;
        [Header("DespawnChecks")]
        public float MaxDistance;
        public float MinVelocity;
        [Header("Graphics")]
        public ProjectileGraphic Prefab;
        public HitMarker HitMarker;
        public HitMarker PenetrateMarker;
        public HitMarker ExitMarker;

        private void OnValidate()
        {
            Damages.Update();
        }
        public float GetDamage(DamageType damageType, float projectileVelocity)
        {
            Damages.Update();
            return Damages[damageType] * Mathf.Max(projectileVelocity / (MaxVelocity * DamageDropoffVelocityPercent), 1);
        }
    }
}