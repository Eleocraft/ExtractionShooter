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
        private int _lastExecutedStateTick = 0; // Serveronly
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
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            _firstPersonController.TransformStateChanged -= TransformStateChanged;
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

            if (states.LastTick <= _lastExecutedStateTick)
                return; // Newer tick already received
            
            _bufferedWeaponInputStates.Insert(states, _lastExecutedStateTick);
        }
        private NetworkWeaponInputState GetPlayerNetworkInputState()
        {
            return new NetworkWeaponInputState(_controls.Mouse.PrimaryAction.IsPressed(),
                _controls.Mouse.SecondaryAction.IsPressed(), _controls.Player.Reload.IsPressed(),
                NetworkManager.ServerTime.Tick, NetworkManager.LocalTime.Tick);
        }
        private void TransformStateChanged(PlayerNetworkTransformState transformState)
        {
            if (IsOwner)
            {
                NetworkWeaponInputState newWeaponInputState = GetPlayerNetworkInputState();
                _bufferedWeaponInputStates.Add(newWeaponInputState);

                OnInputServerRpc((NetworkWeaponInputStateList)_bufferedWeaponInputStates.GetListForTicks(INPUT_TICKS_SEND));
            }
            ExecuteInputs();
        }
        private void ExecuteInputs()
        {
            int maxTickToExecute = Mathf.Min(NetworkManager.LocalTime.Tick, _bufferedWeaponInputStates.LastTick);
            for (int tick = _lastExecutedStateTick + 1; tick <= maxTickToExecute; tick++)
            {
                if (_firstPersonController.GetState(tick, out PlayerNetworkTransformState playerStateAtTick))
                {
                    _bufferedWeaponInputStates[tick]?.SetTickDiff();
                    _playerInventory.ActiveItemObject?.UpdateItem(_bufferedWeaponInputStates[tick], playerStateAtTick);
                }
            }
            
            _lastExecutedStateTick = maxTickToExecute;
        }
    }
}