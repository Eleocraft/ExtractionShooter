using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class AutoloaderRifleMod : RifleItemModifier
    {
        protected override int Id => 1;
        public override int MagSize => 5;

        public override void Shot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            Rifle rifle = _itemObject as Rifle;

            float spray = weaponInputState.SecondaryAction ? SprayADS : Spray;
            Vector3 direction = rifle.GetShootDirection(playerState, spray, rifle.MovementError);
            Projectile.SpawnProjectile(Info, rifle.ShotSource.transform.position, rifle.GetCameraPosition(playerState), direction, rifle.OwnerId, weaponInputState.TickDiff);
            rifle.ShotSource.PlayOneShot(Audio);
        }
    }
}
