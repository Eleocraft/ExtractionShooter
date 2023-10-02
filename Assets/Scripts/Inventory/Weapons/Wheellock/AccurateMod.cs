using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class AccurateModifier : WheellockItemModifier
    {
        public override void SecondShot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            Wheellock wheellock = _itemObject as Wheellock;
            
            float spray = weaponInputState.SecondaryAction ? SprayADS : Spray;
            Vector3 direction = wheellock.GetShootDirection(playerState, spray, wheellock.MovementError);
            Projectile.SpawnProjectile(Info, wheellock.SecondShotSource.position, wheellock.GetCameraPosition(playerState), direction, wheellock.OwnerId, weaponInputState.TickDiff);
            SFXSource.Source.PlayOneShot(Audio);
        }
    }
}
