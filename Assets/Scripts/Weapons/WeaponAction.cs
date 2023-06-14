using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class WeaponAction : NetworkBehaviour
    {
        private enum ActionType { StartMainAction, StopMainAction, StartSecondaryAction, StopSecondaryAction, Utility, Reload }
        [SerializeField] private GlobalInputs GI;
        [SerializeField] private Transform CameraSocket;
        [SerializeField] private Weapon MainWeapon;
        private Weapon _weapon;
        private Dictionary<int, ActionType> _receivedActions; // Serveronly
        private FirstPersonController _firstPersonController; // Serveronly
        private float _cameraYOffset; // Serveronly
        private void Start()
        {
            _weapon = Instantiate(MainWeapon);
            _weapon.OwnerId = OwnerClientId;
            _firstPersonController = GetComponent<FirstPersonController>();
            _cameraYOffset = CameraSocket.localPosition.y;
            NetworkManager.NetworkTickSystem.Tick += Tick;
            if (IsServer)
                _receivedActions = new Dictionary<int, ActionType>();

            if (!IsOwner)
                return;

            GI.Controls.Mouse.MainAction.started += StartMainAction;
            GI.Controls.Mouse.SecondaryAction.started += StartSecondaryAction;
            GI.Controls.Mouse.MainAction.canceled += StopMainAction;
            GI.Controls.Mouse.SecondaryAction.canceled += StopSecondaryAction;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            if (NetworkManager?.NetworkTickSystem != null)
                NetworkManager.NetworkTickSystem.Tick -= Tick;

            if (!IsOwner)
                return;

            GI.Controls.Mouse.MainAction.started -= StartMainAction;
            GI.Controls.Mouse.SecondaryAction.started -= StartSecondaryAction;
            GI.Controls.Mouse.MainAction.canceled -= StopMainAction;
            GI.Controls.Mouse.SecondaryAction.canceled -= StopSecondaryAction;
        }
        private void StartMainAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                PerformAction(ActionType.StartMainAction, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StartMainAction, NetworkManager.LocalTime.Tick);
        }

        private void StopMainAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                PerformAction(ActionType.StopMainAction, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StopMainAction, NetworkManager.LocalTime.Tick);
        }
        private void StartSecondaryAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                PerformAction(ActionType.StartSecondaryAction, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StartSecondaryAction, NetworkManager.LocalTime.Tick);
        }
        private void StopSecondaryAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                PerformAction(ActionType.StopSecondaryAction, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StopSecondaryAction, NetworkManager.LocalTime.Tick);
        }
        [ServerRpc]
        private void ActionServerRpc(ActionType type, int tick)
        {
            if (tick <= NetworkManager.LocalTime.Tick)
                PerformAction(type, tick);
            else
                _receivedActions.Add(tick, type);
        }
        private void PerformAction(ActionType type, int tick)
        {
            if(!_firstPersonController.GetState(tick, out NetworkTransformState transformState)) return;
            
            Vector3 position = transformState.Position + Vector3.up * _cameraYOffset;
            Vector3 direction = Quaternion.AngleAxis(transformState.LookRotation.y, Vector3.up) * (Quaternion.AngleAxis(transformState.LookRotation.x, Vector3.right) * Vector3.up);
            PerformActionAtPos(type, direction, position);

            if (IsServer)
                ActionClientRpc(type, position, direction);
        }
        private void PerformActionAtPos(ActionType type, Vector3 direction, Vector3 position)
        {
            switch (type)
            {
                case ActionType.StartMainAction:
                    _weapon.StartMainAction(position, direction);
                    break;
                case ActionType.StopMainAction:
                    _weapon.StopMainAction();
                    break;
                case ActionType.StartSecondaryAction:
                    _weapon.StartSecondaryAction(position, direction);
                    break;
                case ActionType.StopSecondaryAction:
                    _weapon.StopSecondaryAction();
                    break;
            }
        }
        private void Tick()
        {
            if (IsServer && _receivedActions.ContainsKey(NetworkManager.LocalTime.Tick))
            {
                PerformAction(_receivedActions[NetworkManager.LocalTime.Tick], NetworkManager.LocalTime.Tick);
                _receivedActions.Remove(NetworkManager.LocalTime.Tick);
            }
        }
        [ClientRpc]
        private void ActionClientRpc(ActionType type, Vector3 position, Vector3 direction)
        {
            if (IsOwner) return;

            PerformActionAtPos(type, direction, position);
        }
        private void Update()
        {
            _weapon.UpdateWeapon(CameraSocket.position, CameraSocket.forward);
        }
    }
}