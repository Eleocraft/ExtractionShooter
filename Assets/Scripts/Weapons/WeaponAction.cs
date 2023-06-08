using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class WeaponAction : NetworkBehaviour
    {
        [SerializeField] private GlobalInputs GI;
        [SerializeField] private Transform CameraSocket;
        [SerializeField] private Weapon Weapon;
        private void Start()
        {
            if (!IsOwner)
                return;

            GI.Controls.Player.MainAction.started += StartMainAction;
            GI.Controls.Player.SecondaryAction.started += StartSecondaryAction;
            GI.Controls.Player.MainAction.canceled += StopSecondaryAction;
            GI.Controls.Player.SecondaryAction.canceled += StopSecondaryAction;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            if (!IsOwner)
                return;

            GI.Controls.Player.MainAction.started -= StartMainAction;
            GI.Controls.Player.SecondaryAction.started -= StartSecondaryAction;
            GI.Controls.Player.MainAction.canceled -= StopSecondaryAction;
            GI.Controls.Player.SecondaryAction.canceled -= StopSecondaryAction;
        }
        private void StartMainAction(InputAction.CallbackContext ctx)
        {
            Weapon.StartMainAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
            if (!IsServer)
                StartMainActionServerRpc();
        }
        [ServerRpc]
        private void StartMainActionServerRpc() => Weapon.StartMainAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
        private void StopMainAction(InputAction.CallbackContext ctx)
        {
            Weapon.StopMainAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
            if (!IsServer)
                StopMainActionServerRpc();
        }
        [ServerRpc]
        private void StopMainActionServerRpc() => Weapon.StopMainAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
        private void StartSecondaryAction(InputAction.CallbackContext ctx)
        {
            Weapon.StartSecondaryAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
            if (!IsServer)
                StartSecondaryActionServerRpc();
        }
        [ServerRpc] 
        private void StartSecondaryActionServerRpc() => Weapon.StartSecondaryAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
        private void StopSecondaryAction(InputAction.CallbackContext ctx)
        {
            Weapon.StopSecondaryAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
            if (!IsServer)
                StopSecondaryActionServerRpc();
        }
        [ServerRpc]
        private void StopSecondaryActionServerRpc() => Weapon.StopSecondaryAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
        private void Update()
        {
            if (!IsOwner)
                return;

            Weapon.UpdateWeapon(CameraSocket.position, CameraSocket.forward);
        }
    }
}