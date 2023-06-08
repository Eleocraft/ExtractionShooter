using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "CustomObjects/Weapons/SemiAuto")]
    public class SemiAutomaticWeapon : Weapon
    {
        [SerializeField] private ProjectileInfo projectileInfo;
        [SerializeField] private float Cooldown;
        private float _cooldown;
        public override void StartMainAction(Vector3 position, Vector3 direction)
        {
            if (_cooldown > 0) return;

            Projectile.SpawnProjectile(projectileInfo, position, direction);
            _cooldown = Cooldown;
        }
        public override void UpdateWeapon(Vector3 position, Vector3 direction)
        {
            if (_cooldown > 0)
                _cooldown -= Time.deltaTime;
        }
    }
}