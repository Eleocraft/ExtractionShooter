using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class Weapon : ScriptableObject
    {
        [HideInInspector] public ulong OwnerId;
        [SerializeField] private float MinLockonRange;
        [SerializeField] private float MaxLockonRange;
        public abstract void UpdateWeapon(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState, Vector3 weaponPos, float velocity);
        public virtual void StartPrimaryAction() { }
        public virtual void StopPrimaryAction() { }
        public virtual void StartSecondaryAction() { }
        public virtual void StopSecondaryAction() { }
        
        private const float CAMERA_Y_POSITION = 1.6f;
        protected Vector3 GetShootDirection(Vector3 weaponPosition, NetworkTransformState playerState)
        {
            Vector3 lookDirection = Quaternion.Euler(playerState.LookRotation.x, playerState.LookRotation.y, 0) * Vector3.forward;
            if (Physics.Raycast(Vector3.up * CAMERA_Y_POSITION + playerState.Position, lookDirection, out RaycastHit hitInfo, MaxLockonRange) && hitInfo.distance > MinLockonRange)
                return (hitInfo.point - weaponPosition).normalized;
            
            return lookDirection;
        }
    }
}