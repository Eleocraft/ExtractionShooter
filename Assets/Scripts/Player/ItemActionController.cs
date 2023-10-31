using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ItemActionController : NetworkBehaviour
    {
        [SerializeField] private GlobalInputs GI;

        private FirstPersonController _firstPersonController;
        private PlayerInventory _playerInventory;

        // Owner
        private InputMaster _controls;

        // NetworkWeaponInputStates
        private NetworkWeaponInputStateList _bufferedWeaponInputStates;
        private NetworkWeaponInputState _lastExecutedState; // Serveronly
        const int BUFFER_SIZE = 50;
        private const int INPUT_TICKS_SEND = 30;

        public override void OnNetworkSpawn()
        {
            _firstPersonController = GetComponent<FirstPersonController>();
            _playerInventory = GetComponent<PlayerInventory>();

            if (IsOwner)
                _controls = GI.Controls;
                
            _firstPersonController.TransformStateChanged += TransformStateChanged;
            _bufferedWeaponInputStates = new(BUFFER_SIZE);
            _lastExecutedState = new();
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            _firstPersonController.TransformStateChanged -= TransformStateChanged;
        }
        private void TransformStateChanged(NetworkTransformState transformState)
        {
            if (IsOwner)
            {
                NetworkWeaponInputState newWeaponInputState = GetNetworkInputState();
                _bufferedWeaponInputStates.Add(newWeaponInputState);

                OnInputServerRpc(_bufferedWeaponInputStates.GetListForTicks(INPUT_TICKS_SEND));
            }
            ExecuteInputs();
        }
        [ServerRpc]
        private void OnInputServerRpc(NetworkWeaponInputStateList states)
        {
            OnInputClientRpc(states);
        }
        [ClientRpc]
        private void OnInputClientRpc(NetworkWeaponInputStateList states)
        {
            if (IsOwner)
                return;

            if (states.LastTick <= _lastExecutedState.Tick)
                return; // Newer tick already received
            
            _bufferedWeaponInputStates.Insert(states, _lastExecutedState.Tick);
        }
        private NetworkWeaponInputState GetNetworkInputState()
        {
            return new NetworkWeaponInputState(_controls.Mouse.PrimaryAction.IsPressed(),
                _controls.Mouse.SecondaryAction.IsPressed(), _controls.Player.Reload.IsPressed(),
                NetworkManager.ServerTime.Tick, NetworkManager.LocalTime.Tick);
        }
        private void ExecuteInputs()
        {
            int maxTickToExecute = Mathf.Min(NetworkManager.LocalTime.Tick, _bufferedWeaponInputStates.LastTick);
            for (int tick = _lastExecutedState.Tick + 1; tick <= maxTickToExecute; tick++)
            {
                if (_firstPersonController.GetState(tick, out NetworkTransformState playerStateAtTick))
                {
                    _bufferedWeaponInputStates[tick]?.SetTickDiff();
                    _playerInventory.ActiveItemObject?.UpdateItem(_bufferedWeaponInputStates[tick], playerStateAtTick);
                }
            }
            
            _lastExecutedState = _bufferedWeaponInputStates[maxTickToExecute];
        }
    }
}