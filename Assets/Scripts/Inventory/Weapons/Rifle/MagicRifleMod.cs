using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class MagicRifleMod : RifleItemModifier
    {
        public override int MagSize => 1;
        public override bool CanADS => false;
        [SerializeField] private MagicProjectile Projectile;

        public override bool Shot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState, bool isOwner)
        {
            Rifle rifle = _itemObject as Rifle;

            Vector3 direction = rifle.GetLookDirection(playerState);
            Instantiate(Projectile, rifle.GetCameraPosition(playerState), Quaternion.identity).OnInitialisation(direction, rifle.OwnerId, weaponInputState.TickDiff);
            if (isOwner)
                SFXSource.PlaySoundEffect(Audio);
            else
                rifle.audioSource.PlayOneShot(Audio);
            
            return true;
        }
    }
}
