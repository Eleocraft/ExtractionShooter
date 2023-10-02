using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class MagicRifleMod : RifleItemModifier
    {
        public override int MagSize => 1;
        public override bool CanADS => false;
        [SerializeField] private MagicProjectile Projectile;

        public override bool Shot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            Rifle rifle = _itemObject as Rifle;

            Vector3 direction = rifle.GetLookDirection(playerState);
            Instantiate(Projectile, rifle.GetCameraPosition(playerState), Quaternion.identity).OnInitialisation(direction, rifle.OwnerId, weaponInputState.TickDiff);
            SFXSource.Source.PlayOneShot(Audio);
            
            return true;
        }
    }
}
