using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class Weapon : ScriptableObject
    {
        protected ulong _ownerId;
        [SerializeField] private GameObject WeaponPrefab;

        protected Transform _cameraTransform;

        protected GameObject _weaponObject;

        public virtual void Initialize(ulong ownerId, bool isOwner, Transform weaponTransform, Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
            _ownerId = ownerId;
            if (isOwner)
                _weaponObject = Instantiate(WeaponPrefab, weaponTransform);
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
        
        protected Vector3 GetCameraPosition(NetworkTransformState playerState) => Vector3.up * _cameraTransform.localPosition.y + playerState.Position;
        protected Vector3 GetLookDirection(NetworkTransformState playerState) => Quaternion.Euler(playerState.LookRotation.x, playerState.LookRotation.y, 0) * Vector3.forward;
    }
}