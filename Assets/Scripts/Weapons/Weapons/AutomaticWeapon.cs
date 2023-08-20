using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Auto Weapon", menuName = "CustomObjects/Weapons/Auto")]
    public class AutomaticWeapon : Weapon
    {
        [SerializeField] private ProjectileInfo projectileInfo;
        [SerializeField] private float Cooldown;
        [SerializeField] private float MaxSpray;
        [SerializeField] private float MaxSprayReachTime;
        private float _cooldown;
        private float _relativeSpray;
        private bool _shoot;
        public override void StartMainAction() => _shoot = true;
        public override void StopMainAction() => _shoot = false;
        public override void UpdateWeapon(Vector3 position, Vector3 direction)
        {
            // Spray Calculations
            if (_shoot)
                _relativeSpray += NetworkManager.Singleton.LocalTime.FixedDeltaTime * MaxSprayReachTime;
            else
                _relativeSpray -= NetworkManager.Singleton.LocalTime.FixedDeltaTime * MaxSprayReachTime;

            _relativeSpray = Mathf.Clamp01(_relativeSpray);
            // Cooldown
            if (_cooldown > 0)
                _cooldown -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            // actual shooting
            else if (_shoot)
            {
                // UnityEngine.random can't be used
                Vector3 rotationVector = Vector3.Cross(direction, Random.insideUnitSphere.normalized).normalized;
                Projectile.SpawnProjectile(projectileInfo, position, Quaternion.AngleAxis(_relativeSpray * MaxSpray * Random.value, rotationVector) * direction, OwnerId);
                _cooldown += Cooldown;
            }
        }
    }
}