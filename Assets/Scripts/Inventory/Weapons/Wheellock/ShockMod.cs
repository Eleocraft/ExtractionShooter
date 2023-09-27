using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ShockModifier : WheellockItemModifier
    {
        public override void SecondShot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            Wheellock wheellock = _itemObject as Wheellock;

            float spray = weaponInputState.SecondaryAction ? SprayADS : Spray;
            Vector3 direction = wheellock.GetShootDirection(playerState, spray, wheellock.MovementError);
            Projectile.SpawnProjectile(Info, wheellock.SecondShotSource.transform.position, wheellock.GetCameraPosition(playerState), direction, wheellock.OwnerId, weaponInputState.TickDiff);
            wheellock.SecondShotSource.PlayOneShot(Audio);
        }
    }
}
