using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    // The only reason this wrapper exists is because Netcode can't handle generics
    // This is simply the least scufft way to handle Netcode while still
    // keeping the abstraction between general network player stuff and
    // local gameobject player stuff.
    public abstract class PlayerNetworkController :
        NetworkController<PlayerNetworkInputState, PlayerNetworkTransformState>
    {
        // Buffer
		private NetworkVariable<PlayerNetworkTransformState> _unsafeServerTransformState = new NetworkVariable<PlayerNetworkTransformState>();
		private NetworkVariable<PlayerNetworkTransformState> _safeServerTransformState = new NetworkVariable<PlayerNetworkTransformState>();
        public override void OnNetworkSpawn() {
            base.OnNetworkSpawn();

			_unsafeServerTransformState.OnValueChanged += OnServerStateChanged;
			_safeServerTransformState.OnValueChanged += OnSafeServerStateChanged;
			// Buffers
			_bufferedTransformStates = new(BUFFER_SIZE);
			_bufferedTransformStates.Add(CreateTransformState());
			if (IsOwner || IsServer)
			{
				_bufferedInputStates = new(BUFFER_SIZE);
				_bufferedInputStates.Add(CreateInputState());
			}
        }
        public override void OnDestroy() {
            base.OnDestroy();
			
			_unsafeServerTransformState.OnValueChanged -= OnServerStateChanged;
			_safeServerTransformState.OnValueChanged -= OnSafeServerStateChanged;
        }
		protected override void SetServerTransformStates(PlayerNetworkTransformState transformState)
		{
			_unsafeServerTransformState.Value = transformState;
			_safeServerTransformState.Value = transformState;
		}
        protected override void SetUnsafeServerTransformState(PlayerNetworkTransformState transformState) {
            _unsafeServerTransformState.Value = transformState;
        }
        protected override void SetSafeServerTransformState(PlayerNetworkTransformState transformState) {
            _safeServerTransformState.Value = transformState;
        }
        protected override void SendInput(NetworkStateList<PlayerNetworkInputState> states) => OnInputServerRpc(states as PlayerNetworkInputList);
        [ServerRpc]
        private void OnInputServerRpc(PlayerNetworkInputList inputStates) => base.OnInput(inputStates);
        private void OnSafeServerStateChanged(PlayerNetworkTransformState currentState, PlayerNetworkTransformState receivedState) => base.OnSafeServerStateChanged(receivedState);
        private void OnServerStateChanged(PlayerNetworkTransformState currentState, PlayerNetworkTransformState receivedState) => base.OnServerStateChanged(receivedState);
    }
}
