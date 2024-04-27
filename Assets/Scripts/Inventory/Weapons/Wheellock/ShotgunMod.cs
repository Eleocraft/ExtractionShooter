using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ShotgunModifier : WheellockItemModifier
    {
        [SerializeField] private int ShotgunProjectileAmount;
        public override void SecondShot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState, bool isOwner)
        {
            Wheellock wheellock = _itemObject as Wheellock;

            float spray = weaponInputState.SecondaryAction ? SprayADS : Spray;
            for (int i = 0; i < ShotgunProjectileAmount; i++)
            {
                Vector3 direction = wheellock.GetShootDirection(playerState, spray, 0);
                Projectile.SpawnProjectile(Info, wheellock.SecondShotSource.position, wheellock.GetCameraPosition(playerState), direction, wheellock.OwnerId, weaponInputState.TickDiff);
            }
            if (isOwner)
                SFXSource.PlaySoundEffect(Audio);
            else
                wheellock.audioSource.PlayOneShot(Audio);
        }
    }
}
