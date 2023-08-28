using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class WeaponActionController : NetworkBehaviour
    {
        [SerializeField] private GlobalInputs GI;
        [SerializeField] private Weapon MainWeapon;
        [SerializeField] private Transform WeaponTransform;
        [SerializeField] private float MinLockonRange;
        [SerializeField] private float MaxLockonRange;

        private const float CAMERA_Y_POSITION = 1.6f;
        private Vector3 _weaponPos;
        private FirstPersonController _firstPersonController;

        // Owner
        private InputMaster _controls;
        private Weapon _weapon;
        
        // Server
        private Dictionary<int, NetworkWeaponInputState> _receivedActions; // Serveronly

        // NetworkWeaponInputStates
        private NetworkVariable<NetworkWeaponInputState> _serverWeaponInputState = new NetworkVariable<NetworkWeaponInputState>();
        private NetworkWeaponInputState _currentWeaponInputState = new();

        public override void OnNetworkSpawn()
        {
            _weapon = Instantiate(MainWeapon);
            _weapon.OwnerId = OwnerClientId;
            _firstPersonController = GetComponent<FirstPersonController>();

            _weaponPos = WeaponTransform.position - transform.position;

            if (IsServer)
                _receivedActions = new Dictionary<int, NetworkWeaponInputState>();
            if (IsOwner)
                _controls = GI.Controls;
                
            _firstPersonController.TransformStateChanged += TransformStateChanged;
            _serverWeaponInputState.OnValueChanged += OnServerStateChanged;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            _firstPersonController.TransformStateChanged -= TransformStateChanged;
            _serverWeaponInputState.OnValueChanged -= OnServerStateChanged;
        }
        private void TransformStateChanged(NetworkTransformState transformState)
        {
            if (IsOwner)
            {
                NetworkWeaponInputState newWeaponInputState = GetNetworkInputState();
                // Execute Input
                ExecuteInput(newWeaponInputState);
                if (IsHost)
                    _serverWeaponInputState.Value = newWeaponInputState;
                else
                    OnInputServerRpc(newWeaponInputState);
            }
            else if (IsServer && _receivedActions.ContainsKey(NetworkManager.LocalTime.Tick))
            {
                ExecuteInput(_receivedActions[NetworkManager.LocalTime.Tick]);
                _receivedActions.Remove(NetworkManager.LocalTime.Tick);
                _serverWeaponInputState.Value = _currentWeaponInputState;
            }
            // Update weapon
            Vector3 weaponPosition = transformState.Position + (Quaternion.Euler(transformState.LookRotation.x, transformState.LookRotation.y, 0) * _weaponPos);
            _weapon.UpdateWeapon(_currentWeaponInputState, weaponPosition, GetShootDirection(weaponPosition, transformState.Position, transformState.LookRotation), transformState.Velocity.XZ().magnitude);
        }
        [ServerRpc]
        private void OnInputServerRpc(NetworkWeaponInputState state)
        {
            if (state.Tick == NetworkManager.LocalTime.Tick)
            {
                ExecuteInput(state);
                _serverWeaponInputState.Value = _currentWeaponInputState;
            }
            else if (state.Tick > NetworkManager.LocalTime.Tick && !_receivedActions.ContainsKey(state.Tick))
                _receivedActions.Add(state.Tick, state);
        }
        private void OnServerStateChanged(NetworkWeaponInputState oldState, NetworkWeaponInputState state)
        {
            if (IsServer || IsOwner) return;

            ExecuteInput(state);
        }
        private NetworkWeaponInputState GetNetworkInputState()
        {
            return new NetworkWeaponInputState(_controls.Mouse.PrimaryAction.ReadValue<float>().AsBool(),
                _controls.Mouse.PrimaryAction.ReadValue<float>().AsBool(), NetworkManager.ServerTime.Tick, NetworkManager.LocalTime.Tick);
        }
        private void ExecuteInput(NetworkWeaponInputState weaponInputState)
        {
            if (weaponInputState.PrimaryAction != _currentWeaponInputState.PrimaryAction)
            {
                if (weaponInputState.PrimaryAction)
                    _weapon.StartPrimaryAction();
                else
                    _weapon.StopPrimaryAction();
            }

            if (weaponInputState.SecondaryAction != _currentWeaponInputState.SecondaryAction)
            {
                if (weaponInputState.SecondaryAction)
                    _weapon.StartSecondaryAction();
                else
                    _weapon.StopSecondaryAction();
            }

            _currentWeaponInputState = weaponInputState;
        }
        private Vector3 GetShootDirection(Vector3 weaponPosition, Vector3 playerPosition, Vector2 lookRotation)
        {
            Vector3 lookDirection = Quaternion.Euler(lookRotation.x, lookRotation.y, 0) * Vector3.forward;
            if (Physics.Raycast(Vector3.up * CAMERA_Y_POSITION + playerPosition, lookDirection, out RaycastHit hitInfo, MaxLockonRange) && hitInfo.distance > MinLockonRange)
                return (hitInfo.point - weaponPosition).normalized;
            
            return lookDirection;
        }
    }
}