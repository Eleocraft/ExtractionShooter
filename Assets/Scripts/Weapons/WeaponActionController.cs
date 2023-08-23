using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class WeaponActionController : NetworkBehaviour
    {
        private enum ActionType { StartMainAction, StopMainAction, StartSecondaryAction, StopSecondaryAction, Utility, Reload }
        [SerializeField] private GlobalInputs GI;
        [SerializeField] private Transform CameraSocket;
        [SerializeField] private Weapon MainWeapon;
        private Weapon _weapon;
        private Dictionary<int, ActionType> _receivedActions; // Serveronly
        private FirstPersonController _firstPersonController; // Serveronly
        private float _cameraYOffset; // Serveronly

        private bool _mainActionStopped;
        private bool _secondaryActionStopped;
        
        private int _weaponTickDiff;
        public override void OnNetworkSpawn()
        {
            _weapon = Instantiate(MainWeapon);
            _weapon.OwnerId = OwnerClientId;
            _firstPersonController = GetComponent<FirstPersonController>();
            _cameraYOffset = CameraSocket.localPosition.y - 0.01f;
            if (IsServer)
                _receivedActions = new Dictionary<int, ActionType>();
            
            _firstPersonController.TransformStateChanged += TransformStateChanged;

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

            _firstPersonController.TransformStateChanged -= TransformStateChanged;

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
                CalculateAction(ActionType.StartMainAction, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StartMainAction, NetworkManager.LocalTime.Tick, NetworkManager.ServerTime.Tick);
        }

        private void StopMainAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                CalculateAction(ActionType.StopMainAction, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StopMainAction, NetworkManager.LocalTime.Tick, NetworkManager.ServerTime.Tick);
        }
        private void StartSecondaryAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                CalculateAction(ActionType.StartSecondaryAction, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StartSecondaryAction, NetworkManager.LocalTime.Tick, NetworkManager.ServerTime.Tick);
        }
        private void StopSecondaryAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                CalculateAction(ActionType.StopSecondaryAction, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StopSecondaryAction, NetworkManager.LocalTime.Tick, NetworkManager.ServerTime.Tick);
        }
        [ServerRpc]
        private void ActionServerRpc(ActionType type, int tick, int serverTick)
        {
            _weaponTickDiff = NetworkManager.ServerTime.Tick - serverTick;

            if (tick <= NetworkManager.LocalTime.Tick)
                CalculateAction(type, tick);
            else
                _receivedActions.Add(tick, type);
        }
        private void CalculateAction(ActionType type, int tick)
        {
            if(!_firstPersonController.GetState(tick, out NetworkTransformState transformState)) return;
            
            Vector3 position = GetShootPosition(transformState.Position);
            Vector3 direction = GetShootDirection(transformState.LookRotation);
            PerformAction(type, direction, position);

            if (IsServer)
                ActionClientRpc(type, position, direction);
        }
        private Vector3 GetShootPosition(Vector3 playerPosition) => playerPosition + Vector3.up * _cameraYOffset;
        private Vector3 GetShootDirection(Vector2 lookRotation) => Quaternion.Euler(lookRotation.x, lookRotation.y, 0) * Vector3.forward;
        private void PerformAction(ActionType type, Vector3 direction, Vector3 position)
        {
            switch (type)
            {
                case ActionType.StartMainAction:
                    _weapon.StartMainAction();
                    break;
                case ActionType.StopMainAction:
                    _mainActionStopped = true;
                    break;
                case ActionType.StartSecondaryAction:
                    _weapon.StartSecondaryAction();
                    break;
                case ActionType.StopSecondaryAction:
                    _secondaryActionStopped = true;
                    break;
            }
        }
        private void TransformStateChanged(NetworkTransformState transformState)
        {
            if (IsServer && _receivedActions.ContainsKey(NetworkManager.LocalTime.Tick))
            {
                CalculateAction(_receivedActions[NetworkManager.LocalTime.Tick], NetworkManager.LocalTime.Tick);
                _receivedActions.Remove(NetworkManager.LocalTime.Tick);
            }
            // Update weapon
            _weapon.UpdateWeapon(GetShootPosition(transformState.Position), GetShootDirection(transformState.LookRotation), transformState.Velocity.XZ().magnitude, _weaponTickDiff);

            // actions are only stopped after one weapon update
            if (_mainActionStopped)
            {
                _weapon.StopMainAction();
                _mainActionStopped = false;
            }
            if (_secondaryActionStopped)
            {
                _weapon.StopSecondaryAction();
                _secondaryActionStopped = false;
            }
        }
        [ClientRpc]
        private void ActionClientRpc(ActionType type, Vector3 position, Vector3 direction)
        {
            if (IsOwner) return;

            PerformAction(type, direction, position);
        }
    }
}