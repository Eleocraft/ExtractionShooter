using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Wheellock Modifier", menuName = "CustomObjects/Weapons/Wheellock/Shotgun")]
    public class ShotgunModifier : WheellockItemModifier
    {
        protected override int Id => 3;
        [SerializeField] private int ShotgunProjectileAmount;
        public override void SecondShot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState, Wheellock wheellock)
        {
            float spray = weaponInputState.SecondaryAction ? SprayADS : Spray;
            for (int i = 0; i < ShotgunProjectileAmount; i++)
            {
                Vector3 direction = wheellock.GetShootDirection(playerState, spray, wheellock.MovementError);
                Projectile.SpawnProjectile(Info, wheellock.SecondShotSource.position, wheellock.GetCameraPosition(playerState), direction, wheellock.OwnerId, weaponInputState.TickDiff);
            }
            wheellock.GunAudioSource[1].PlayOneShot(Audio);
        }
    }
}
