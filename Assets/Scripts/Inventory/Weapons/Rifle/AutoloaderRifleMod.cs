using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class AutoloaderRifleMod : RifleItemModifier
    {
        public override int MagSize => MagazineSize;

        [SerializeField] private int MagazineSize;
        [SerializeField] private float Cooldown;
        [SerializeField] private float SprayIncreasePerBullet;
        [SerializeField] private float SprayDecreaseSpeed;

        private float _cooldown;
        private float _relativeSpray;
        public override bool Shot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState, bool isOwner)
        {
            if (_cooldown > 0) return false;

            Rifle rifle = _itemObject as Rifle;

            float spray = _relativeSpray * (weaponInputState.SecondaryAction ? SprayADS : Spray);
            Vector3 direction = rifle.GetShootDirection(playerState, spray, rifle.MovementError);
            Projectile.SpawnProjectile(Info, rifle.ShotSource.position, rifle.GetCameraPosition(playerState), direction, rifle.OwnerId, weaponInputState.TickDiff);
            if (isOwner)
                SFXSource.Source.PlayOneShot(Audio);
            else
                rifle.audioSource.PlayOneShot(Audio);

            _cooldown = Cooldown;
            _relativeSpray += SprayIncreasePerBullet;
            return true;
        }
        public override void UpdateItem(bool ADS)
        {
            base.Activate();
            if (_cooldown > 0)
                _cooldown -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            else if (_relativeSpray > 0)
                _relativeSpray -= NetworkManager.Singleton.LocalTime.FixedDeltaTime * SprayDecreaseSpeed;
            _relativeSpray = Mathf.Clamp01(_relativeSpray);
        }
    }
}
