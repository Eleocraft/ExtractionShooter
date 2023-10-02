using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class DefaultRifleMod : RifleItemModifier
    {
        public override int MagSize => 1;

        public override bool Shot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            Rifle rifle = _itemObject as Rifle;

            float spray = weaponInputState.SecondaryAction ? SprayADS : Spray;
            Vector3 direction = rifle.GetShootDirection(playerState, spray, rifle.MovementError);
            Projectile.SpawnProjectile(Info, rifle.ShotSource.position, rifle.GetCameraPosition(playerState), direction, rifle.OwnerId, weaponInputState.TickDiff);
            SFXSource.Source.PlayOneShot(Audio);
            
            return true;
        }
    }
}
