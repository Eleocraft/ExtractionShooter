using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Granade Weapon", menuName = "CustomObjects/Utility/Granade")]
    public class Granade : UtilityItem
    {
        [SerializeField] private ThrowableInfo throwableInfo;
        private bool _throw;
        public override void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            if (!_throw) return;

            Throwable.SpawnProjectile(throwableInfo, GetCameraPosition(playerState), GetLookDirection(playerState), _ownerId, weaponInputState.TickDiff);
            _throw = false;
        }

        public override void UseUtility()
        {
            _throw = true;
        }
    }
}
