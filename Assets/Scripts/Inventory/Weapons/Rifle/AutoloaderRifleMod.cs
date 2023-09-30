using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class AutoloaderRifleMod : RifleItemModifier
    {
        public override int MagSize => MagazineSize;

        [SerializeField] private int MagazineSize;
        [SerializeField] private float Cooldown;

        private float _cooldown;

        public override bool Shot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            if (_cooldown > 0) return false;

            Rifle rifle = _itemObject as Rifle;

            float spray = weaponInputState.SecondaryAction ? SprayADS : Spray;
            Vector3 direction = rifle.GetShootDirection(playerState, spray, rifle.MovementError);
            Projectile.SpawnProjectile(Info, rifle.ShotSource.transform.position, rifle.GetCameraPosition(playerState), direction, rifle.OwnerId, weaponInputState.TickDiff);
            rifle.ShotSource.PlayOneShot(Audio);

            _cooldown = Cooldown;
            return true;
        }
        public override void UpdateItem()
        {
            base.Activate();
            if (_cooldown > 0)
                _cooldown -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
        }
    }
}
