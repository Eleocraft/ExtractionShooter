using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class WeaponActionController : NetworkBehaviour
    {
        [SerializeField] private GlobalInputs GI;
        [SerializeField] private Weapon MainWeapon;
        [SerializeField] private UtilityItem UtilityItem;

        private FirstPersonController _firstPersonController;

        // Owner
        private InputMaster _controls;
        private Weapon _weapon;
        private UtilityItem _utilityItem;
        
        // Server
        private Dictionary<int, NetworkWeaponInputState> _receivedActions; // Serveronly

        // NetworkWeaponInputStates
        private NetworkVariable<NetworkWeaponInputState> _serverWeaponInputState = new NetworkVariable<NetworkWeaponInputState>();
        private NetworkWeaponInputState _currentWeaponInputState = new();

        public override void OnNetworkSpawn()
        {
            _firstPersonController = GetComponent<FirstPersonController>();
            _weapon = Instantiate(MainWeapon);
            _weapon.Initialize(OwnerClientId, IsOwner, _firstPersonController);
            _weapon.Activate();

            _utilityItem = Instantiate(UtilityItem);
            _utilityItem.Initialize(OwnerClientId, IsOwner, _firstPersonController);

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
            _weapon.UpdateWeapon(_currentWeaponInputState, transformState);
            _utilityItem.UpdateItem(_currentWeaponInputState, transformState);

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

            ExecuteInput((NetworkWeaponInputState)state.GetStateWithTick(state.Tick));
        }
        private NetworkWeaponInputState GetNetworkInputState()
        {
            return new NetworkWeaponInputState(_controls.Mouse.PrimaryAction.IsPressed(),
                _controls.Mouse.SecondaryAction.IsPressed(), _controls.Player.Reload.IsPressed(), 
                _controls.Player.Utility.IsPressed(), NetworkManager.ServerTime.Tick, NetworkManager.LocalTime.Tick);
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

            if (weaponInputState.ReloadAction && !_currentWeaponInputState.ReloadAction)
                _weapon.Reload();
            
            if (weaponInputState.UtilityAction && !_currentWeaponInputState.UtilityAction)
                _utilityItem.UseUtility();

            _currentWeaponInputState = weaponInputState;
        }
    }
}