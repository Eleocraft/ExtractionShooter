using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "CustomObjects/Weapons/SemiAuto")]
    public class SemiAutomaticWeapon : Weapon
    {
        [SerializeField] private ProjectileInfo projectileInfo;
        [SerializeField] private float Cooldown;
        private float _cooldown;
        private bool _shoot;
        public override void StartMainAction()
        {
            _shoot = true;
        }
        public override void UpdateWeapon(Vector3 position, Vector3 direction, float velocity, int tick)
        {
            if (_cooldown > 0)
                _cooldown -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            else if (_shoot)
            {
                Projectile.SpawnProjectile(projectileInfo, position, direction, OwnerId, tick);
                _cooldown = Cooldown;
                _shoot = false;
            }
        }
    }
}