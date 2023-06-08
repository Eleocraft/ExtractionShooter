using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class WeaponAction : NetworkBehaviour
    {
        [SerializeField] private GlobalInputs GI;
        [SerializeField] private Transform CameraSocket;
        [SerializeField] private Weapon MainWeapon;
        private Weapon _weapon;
        private void Start()
        {
            _weapon = Instantiate(MainWeapon);
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
            if (!IsServer)
                _weapon.StartMainAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
            StartMainActionServerRpc();
        }
        [ServerRpc]
        private void StartMainActionServerRpc()
        {
            Vector3 direction = CameraSocket.rotation * Vector3.forward;
            _weapon.StartMainAction(CameraSocket.position, direction);
            StartMainActionClientRpc(CameraSocket.position, direction);
        }
        [ClientRpc]
        private void StartMainActionClientRpc(Vector3 position, Vector3 direction)
        {
            if (!IsOwner)
                _weapon.StartMainAction(position, direction);
        }

        private void StopMainAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                _weapon.StopMainAction();
            StopMainActionServerRpc();
        }
        [ServerRpc]
        private void StopMainActionServerRpc()
        {
            _weapon.StopMainAction();
            StopMainActionClientRpc();
        }
        [ClientRpc]
        private void StopMainActionClientRpc()
        {
            if (!IsOwner)
                _weapon.StopMainAction();
        }
        private void StartSecondaryAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                _weapon.StartSecondaryAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
            StartSecondaryActionServerRpc();
        }
        [ServerRpc] 
        private void StartSecondaryActionServerRpc()
        {
            Vector3 direction = CameraSocket.rotation * Vector3.forward;
            _weapon.StartSecondaryAction(CameraSocket.position, direction);
            StartSecondaryActionClientRpc(CameraSocket.position, direction);
        }
        [ClientRpc]
        private void StartSecondaryActionClientRpc(Vector3 position, Vector3 direction)
        {
            if (!IsOwner)
                _weapon.StartSecondaryAction(position, direction);
        }
        private void StopSecondaryAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                _weapon.StopSecondaryAction();
            StopSecondaryActionServerRpc();
        }
        [ServerRpc]
        private void StopSecondaryActionServerRpc()
        {
            _weapon.StopSecondaryAction();
            StopSecondaryActionClientRpc();
        }
        [ClientRpc]
        private void StopSecondaryActionClientRpc()
        {
            if (!IsOwner)
                _weapon.StopSecondaryAction();
        }
        private void Update()
        {
            _weapon.UpdateWeapon(CameraSocket.position, CameraSocket.forward);
        }
    }
}