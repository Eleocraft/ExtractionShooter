using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class AutomaticWeapon : ADSWeapon
    {
        protected override float ADSFOV => 50;
        protected override Vector3 ADSPos => new Vector3(0, -0.05f, 0.4f);
        [SerializeField] private ProjectileInfo projectileInfo;
        [SerializeField] private float Cooldown;
        [SerializeField] [MinMaxRange(0, 5)] private RangeSlider Spray;
        [SerializeField] [MinMaxRange(0, 5)] private RangeSlider ADSSpray;
        [SerializeField] private float ShotsUntilMaxSpray;
        [SerializeField] private float SprayResetTime;
        [SerializeField] private float MovementError;
        [SerializeField] private int MagazineSize;
        [SerializeField] private float TimeToReload;
        [SerializeField] private float MaxRecoil;
        [SerializeField] private float RelativeRecoil;

        [Header("WeaponObject")]
        [SerializeField] private Transform ShotSource;

        private float _cooldown;
        private float _relativeSpray;

        private float _sprayDecreaseSpeed;
        
        
        private float _sprayIncreaseByShot;
        public override int MagSize => MagazineSize;
        public override float ReloadTime => TimeToReload;

        public override void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller)
        {
            base.Initialize(ownerId, isOwner, controller);

            _sprayIncreaseByShot = 1f / ShotsUntilMaxSpray;
            _sprayDecreaseSpeed = 1f / SprayResetTime;
        }
        public override void Activate()
        {
            base.Activate();
        }
        public override void Deactivate()
        {
            base.Deactivate();

            _relativeSpray = 0;
            _cooldown = 0;
        }
        public override void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            base.UpdateItem(weaponInputState, playerState);

            // Cooldown
            if (_cooldown > 0)
                _cooldown -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            // actual shooting
            else if (weaponInputState.PrimaryAction && !InADSTransit && BulletsLoaded > 0 && !IsReloading)
            {
                float spray = weaponInputState.SecondaryAction ? ADSSpray.Evaluate(_relativeSpray) : Spray.Evaluate(_relativeSpray);
                Vector3 direction = GetShootDirection(playerState, spray, MovementError);

                Projectile.SpawnProjectile(projectileInfo, ShotSource.position, GetCameraPosition(playerState), direction, OwnerId, weaponInputState.TickDiff);
                _cooldown += Cooldown;
                BulletsLoaded--;
                _relativeSpray += _sprayIncreaseByShot;
                _recoil += RelativeRecoil * (1f - (_recoil / MaxRecoil));
            }
            else
                _relativeSpray -= NetworkManager.Singleton.LocalTime.FixedDeltaTime * _sprayDecreaseSpeed;

            _relativeSpray = Mathf.Clamp01(_relativeSpray);
        }
    }
}