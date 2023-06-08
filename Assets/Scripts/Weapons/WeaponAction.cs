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
        private void StartMainActionServerRpc()
        {
            Vector3 direction = CameraSocket.rotation * Vector3.forward;
            Weapon.StartMainAction(CameraSocket.position, direction);
            StartMainActionClientRpc(CameraSocket.position, direction);

        }
        [ClientRpc]
        private void StartMainActionClientRpc(Vector3 position, Vector3 direction)
        {
            if (!IsOwner)
                Weapon.StartMainAction(position, direction);
        }

        private void StopMainAction(InputAction.CallbackContext ctx)
        {
            Weapon.StopMainAction();
            if (!IsServer)
                StopMainActionServerRpc();
        }
        [ServerRpc]
        private void StopMainActionServerRpc()
        {
            Weapon.StopMainAction();
            StopMainActionClientRpc();
        }
        [ClientRpc]
        private void StopMainActionClientRpc()
        {
            if (!IsOwner)
                Weapon.StopMainAction();
        }
        private void StartSecondaryAction(InputAction.CallbackContext ctx)
        {
            Weapon.StartSecondaryAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
            if (!IsServer)
                StartSecondaryActionServerRpc();
        }
        [ServerRpc] 
        private void StartSecondaryActionServerRpc()
        {
            Vector3 direction = CameraSocket.rotation * Vector3.forward;
            Weapon.StartSecondaryAction(CameraSocket.position, direction);
            StartSecondaryActionClientRpc(CameraSocket.position, direction);
        }
        [ClientRpc]
        private void StartSecondaryActionClientRpc(Vector3 position, Vector3 direction)
        {
            if (!IsOwner)
                Weapon.StartSecondaryAction(position, direction);
        }
        private void StopSecondaryAction(InputAction.CallbackContext ctx)
        {
            Weapon.StopSecondaryAction();
            if (!IsServer)
                StopSecondaryActionServerRpc();
        }
        [ServerRpc]
        private void StopSecondaryActionServerRpc()
        {
            Weapon.StopSecondaryAction();
            StopSecondaryActionClientRpc();
        }
        [ClientRpc]
        private void StopSecondaryActionClientRpc()
        {
            if (!IsOwner)
                Weapon.StopSecondaryAction();
        }
        private void Update()
        {
            if (!IsOwner)
                return;

            Weapon.UpdateWeapon(CameraSocket.position, CameraSocket.forward);
        }
    }
}