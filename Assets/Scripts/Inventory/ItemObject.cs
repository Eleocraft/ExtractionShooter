using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class ItemObject : ScriptableObject
    {
        FirstPersonController _controller;
        protected ulong _ownerId;
        protected bool _isOwner;
        [ReadOnly] public string ItemID;
        public float VelocityMultiplier;
        
        protected Transform _cameraTransform;
        public virtual void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller) {
            
            _controller = controller;
            _cameraTransform = controller.PlayerModel.CameraSocket;
            _ownerId = ownerId;
            _isOwner = isOwner;
        }
        private void OnValidate()
        {
            ItemID = Utility.CreateID(name);
        }
        public virtual void Activate() {
            _controller.IncreaseMovementVelocityMultiplier(VelocityMultiplier);
        }
        public virtual void Deactivate() {
            _controller.DecreaseMovementVelocityMultiplier(VelocityMultiplier);
        }
        public abstract void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState);
        
        public virtual void StartPrimaryAction() { }
        public virtual void StopPrimaryAction() { }
        public virtual void StartSecondaryAction() { }
        public virtual void StopSecondaryAction() { }

        public virtual void Reload() { }

        protected Vector3 GetCameraPosition(NetworkTransformState playerState) => Vector3.up * _cameraTransform.localPosition.y + playerState.Position;
        protected Vector3 GetLookDirection(NetworkTransformState playerState) => Quaternion.Euler(playerState.LookRotation.x, playerState.LookRotation.y, 0) * Vector3.forward;
    }
}
