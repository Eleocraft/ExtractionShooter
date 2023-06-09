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
        private Dictionary<int, PerformedAction> _receivedActions = new Dictionary<int, PerformedAction>(); // Serveronly
        private FirstPersonController _firstPersonController; // Serveronly
        private float _cameraYOffset; // Serveronly
        private Vector3 LocalDirection => CameraSocket.rotation * Vector3.forward;
        private void Start()
        {
            _weapon = Instantiate(MainWeapon);
            _firstPersonController = GetComponent<FirstPersonController>();
            _cameraYOffset = CameraSocket.localPosition.y;
            NetworkManager.NetworkTickSystem.Tick += Tick;
            if (!IsOwner)
                return;

            _weapon.Friendly = true;
            GI.Controls.Mouse.MainAction.started += StartMainAction;
            GI.Controls.Mouse.SecondaryAction.started += StartSecondaryAction;
            GI.Controls.Mouse.MainAction.canceled += StopSecondaryAction;
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
            GI.Controls.Mouse.MainAction.canceled -= StopSecondaryAction;
            GI.Controls.Mouse.SecondaryAction.canceled -= StopSecondaryAction;
        }
        private void StartMainAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                PerformAction(ActionType.StartMainAction, LocalDirection, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StartMainAction, LocalDirection, NetworkManager.LocalTime.Tick);
        }

        private void StopMainAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                PerformAction(ActionType.StopMainAction, LocalDirection, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StopMainAction, LocalDirection, NetworkManager.LocalTime.Tick);
        }
        private void StartSecondaryAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                PerformAction(ActionType.StartSecondaryAction, LocalDirection, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StartSecondaryAction, LocalDirection, NetworkManager.LocalTime.Tick);
        }
        private void StopSecondaryAction(InputAction.CallbackContext ctx)
        {
            if (!IsServer)
                PerformAction(ActionType.StopSecondaryAction, LocalDirection, NetworkManager.LocalTime.Tick);
            ActionServerRpc(ActionType.StopSecondaryAction, LocalDirection, NetworkManager.LocalTime.Tick);
        }
        [ServerRpc]
        private void ActionServerRpc(ActionType type, Vector3 direction, int tick)
        {
            if (tick <= NetworkManager.LocalTime.Tick)
                PerformAction(type, direction, tick);
            else
                _receivedActions.Add(tick, new(type, direction));
        }
        private void PerformAction(ActionType type, Vector3 direction, int tick)
        {
            if(!_firstPersonController.GetState(tick, out NetworkTransformState transformState)) return;
                
            Vector3 position = transformState.Position + Vector3.up * _cameraYOffset;
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
                PerformAction(_receivedActions[NetworkManager.LocalTime.Tick].Type, 
                    _receivedActions[NetworkManager.LocalTime.Tick].Direction, NetworkManager.LocalTime.Tick);

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
        private class PerformedAction
        {
            public ActionType Type;
            public Vector3 Direction;
            public PerformedAction(ActionType type, Vector3 direction)
            {
                Type = type;
                Direction = direction;
            }
        }
    }
}