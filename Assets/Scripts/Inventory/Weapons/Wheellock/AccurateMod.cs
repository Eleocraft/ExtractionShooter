using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Wheellock Modifier", menuName = "CustomObjects/Weapons/Wheellock/AccurateMod")]
    public class AccurateModifier : WheellockItemModifier
    {
        protected override int Id => 2;
        public override void SecondShot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState, Wheellock wheellock)
        {
            float spray = weaponInputState.SecondaryAction ? SprayADS : Spray;
            Vector3 direction = wheellock.GetShootDirection(playerState, spray, wheellock.MovementError);
            Projectile.SpawnProjectile(Info, wheellock.SecondShotSource.position, wheellock.GetCameraPosition(playerState), direction, wheellock.OwnerId, weaponInputState.TickDiff);
            wheellock.GunAudioSource[1].PlayOneShot(Audio);
        }
    }
}
