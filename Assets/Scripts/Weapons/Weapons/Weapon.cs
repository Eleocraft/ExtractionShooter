using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class Weapon : ScriptableObject
    {
        protected ulong _ownerId;
        [SerializeField] private GameObject WeaponPrefab;
        [SerializeField] private float MinLockonRange;
        [SerializeField] private float MaxLockonRange;

        private Vector3 _relativeWeaponPos;
        protected GameObject _weaponObject;

        public virtual void Initialize(ulong ownerId, bool isOwner, Transform weaponPos)
        {
            _relativeWeaponPos = weaponPos.localPosition;
            _ownerId = ownerId;
            if (isOwner)
                _weaponObject = Instantiate(WeaponPrefab, weaponPos);
        }
        private void OnDestroy()
        {
            Destroy(_weaponObject);
        }
        public abstract void UpdateWeapon(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState);
        public virtual void StartPrimaryAction() { }
        public virtual void StopPrimaryAction() { }
        public virtual void StartSecondaryAction() { }
        public virtual void StopSecondaryAction() { }
        
        private const float CAMERA_Y_POSITION = 1.6f;
        protected Vector3 GetCameraPosition(NetworkTransformState playerState) => Vector3.up * CAMERA_Y_POSITION + playerState.Position;
        protected Vector3 GetWeaponPosition(NetworkTransformState playerState) => GetCameraPosition(playerState) + (Quaternion.Euler(playerState.LookRotation.x, playerState.LookRotation.y, 0) * _relativeWeaponPos);
        protected Vector3 GetLookDirection(NetworkTransformState playerState) => Quaternion.Euler(playerState.LookRotation.x, playerState.LookRotation.y, 0) * Vector3.forward;
        protected Vector3 GetShootDirection(NetworkTransformState playerState)
        {
            Vector3 cameraPosition = GetCameraPosition(playerState);
            Vector3 weaponPosition = GetWeaponPosition(playerState);

            Vector3 lookDirection = GetLookDirection(playerState);
            if (Physics.Raycast(cameraPosition, lookDirection, out RaycastHit hitInfo, MaxLockonRange) && hitInfo.distance > MinLockonRange)
                return (hitInfo.point - weaponPosition).normalized;
            
            return (cameraPosition + lookDirection * MaxLockonRange - weaponPosition).normalized;
        }
    }
}