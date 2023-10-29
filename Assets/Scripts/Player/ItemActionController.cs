using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ItemActionController : NetworkBehaviour
    {
        [SerializeField] private GlobalInputs GI;

        private FirstPersonController _firstPersonController;
        private PlayerInventory _playerInventory;

        // Owner
        private InputMaster _controls;
        
        // Server
        private Dictionary<int, NetworkWeaponInputState> _receivedActions; // Serveronly

        // NetworkWeaponInputStates
        private NetworkVariable<NetworkWeaponInputState> _serverWeaponInputState = new NetworkVariable<NetworkWeaponInputState>();
        private NetworkWeaponInputState _currentWeaponInputState = new();

        public override void OnNetworkSpawn()
        {
            _firstPersonController = GetComponent<FirstPersonController>();
            _playerInventory = GetComponent<PlayerInventory>();

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
                ExecuteAction(newWeaponInputState, transformState);
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
        }
        [ServerRpc]
        private void OnInputServerRpc(NetworkWeaponInputState state)
        {
            if (state.Tick < _currentWeaponInputState.Tick)
                return; // Newer tick already received

            if (state.Tick <= NetworkManager.LocalTime.Tick)
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
            return new NetworkWeaponInputState(_controls.Mouse.PrimaryAction.IsPressed(),
                _controls.Mouse.SecondaryAction.IsPressed(), _controls.Player.Reload.IsPressed(),
                NetworkManager.ServerTime.Tick, NetworkManager.LocalTime.Tick);
        }
        
        private void ExecuteInput(NetworkWeaponInputState newWeaponInputState)
        {
            newWeaponInputState.SetTickDiff();
            for (int tick = _currentWeaponInputState.Tick + 1; tick < newWeaponInputState.Tick; tick++)
                    if (_firstPersonController.GetState(tick, out NetworkTransformState playerStateAtTick))
                        ExecuteAction(_currentWeaponInputState, playerStateAtTick);

            if (_firstPersonController.GetState(newWeaponInputState.Tick, out NetworkTransformState playerState))
                ExecuteAction(newWeaponInputState, playerState);
        }
        private void ExecuteAction(NetworkWeaponInputState weaponInputState, NetworkTransformState transformState)
        {
            if (weaponInputState.PrimaryAction != _currentWeaponInputState.PrimaryAction)
            {
                if (weaponInputState.PrimaryAction)
                    _playerInventory.ActiveItemObject?.StartPrimaryAction();
                else
                    _playerInventory.ActiveItemObject?.StopPrimaryAction();
            }

            if (weaponInputState.SecondaryAction != _currentWeaponInputState.SecondaryAction)
            {
                if (weaponInputState.SecondaryAction)
                    _playerInventory.ActiveItemObject?.StartSecondaryAction();
                else
                    _playerInventory.ActiveItemObject?.StopSecondaryAction();
            }

            if (weaponInputState.ReloadAction && !_currentWeaponInputState.ReloadAction)
                _playerInventory.ActiveItemObject?.Reload();

            _currentWeaponInputState = weaponInputState;
            // Update weapon
            _playerInventory.ActiveItemObject?.UpdateItem(_currentWeaponInputState, transformState);
        }
    }
}