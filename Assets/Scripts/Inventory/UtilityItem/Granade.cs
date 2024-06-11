using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Granade : UtilityItem
    {
        [SerializeField] private ThrowableInfo throwableInfo;
        [SerializeField] private float Cooldown;
        [SerializeField] private float ThrowVelocity;
        private float _cooldown;
        private bool _threw;
        public override void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            if (_threw && !weaponInputState.PrimaryAction)
                _threw = false;

            if (_cooldown > 0)
                _cooldown -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;

            else if (weaponInputState.PrimaryAction && !_threw)
            {
                Throwable.SpawnProjectile(throwableInfo, GetCameraPosition(playerState), GetLookDirection(playerState) * ThrowVelocity, OwnerId, weaponInputState.TickDiff);
                _threw = true;
                _cooldown = Cooldown;
            }
        }
    }
}
