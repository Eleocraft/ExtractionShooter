using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Auto Weapon", menuName = "CustomObjects/Weapons/Auto")]
    public class AutomaticWeapon : Weapon
    {
        [SerializeField] private ProjectileInfo projectileInfo;
        [SerializeField] private float Cooldown;
        private float _cooldown;
        private bool _active;
        public override void StartMainAction(Vector3 position, Vector3 direction)
        {
            _active = true;
        }
        public override void StopMainAction()
        {
            _active = false;
        }
        public override void UpdateWeapon(Vector3 position, Vector3 direction)
        {
            if (!_active) return;

            if (_cooldown > 0)
                _cooldown -= Time.deltaTime;
            else
            {
                Projectile.SpawnProjectile(projectileInfo, position, direction, OwnerId);
                _cooldown += Cooldown;
            }
        }
    }
}