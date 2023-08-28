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
        public override void StartPrimaryAction()
        {
            _shoot = true;
        }
        public override void Initialize(bool IsOwner) { }
        public override void UpdateWeapon(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState, Vector3 weaponPos, float velocity)
        {
            if (_cooldown > 0)
                _cooldown -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            else if (_shoot)
            {
                Projectile.SpawnProjectile(projectileInfo, weaponPos, GetShootDirection(weaponPos, playerState), OwnerId, weaponInputState.TickDiff);
                _cooldown = Cooldown;
                _shoot = false;
            }
        }
    }
}