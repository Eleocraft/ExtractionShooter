using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class UtilityItem : ScriptableObject
    {
        private Transform _cameraTransform;
        protected ulong _ownerId;
        public virtual void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller) {
            _cameraTransform = controller.PlayerModel.CameraSocket;
            _ownerId = ownerId;
        }
        public abstract void UseUtility();
        public abstract void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState);
        
        protected Vector3 GetCameraPosition(NetworkTransformState playerState) => Vector3.up * _cameraTransform.localPosition.y + playerState.Position;
        protected Vector3 GetLookDirection(NetworkTransformState playerState) => Quaternion.Euler(playerState.LookRotation.x, playerState.LookRotation.y, 0) * Vector3.forward;
    }
}
