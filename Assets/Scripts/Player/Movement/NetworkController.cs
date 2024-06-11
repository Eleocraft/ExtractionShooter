using UnityEngine;
using Unity.Netcode;
using System;

namespace ExoplanetStudios.ExtractionShooter
{
	public abstract class NetworkController<I,T>  : NetworkBehaviour where I : NetworkState where T : NetworkState
	{
		protected NetworkStateList<T> _bufferedTransformStates;
		protected NetworkStateList<I> _bufferedInputStates; // Owner and server
		protected T _currentTransformState; // The current transform state

		protected I _lastSaveInput; // Serveronly
		protected T _lastSaveTransform; // Serveronly

		// Constants
		protected const int BUFFER_SIZE = 200;
		protected const int INPUT_TICKS_SEND = 30;

		public Action<T> TransformStateChanged;

		public override void OnNetworkSpawn()
		{
			// Subscribe to tick and OnServerStateChanged events
			NetworkManager.NetworkTickSystem.Tick += Tick;
			// reset Network Variables
            _currentTransformState = CreateTransformState();
			if (IsServer)
			{
				SetServerTransformStates(CreateTransformState());
				_lastSaveInput = CreateInputState();
				_lastSaveTransform = CreateTransformState();
			}
		}
		public override void OnDestroy()
		{
			base.OnDestroy();

			if (NetworkManager?.NetworkTickSystem != null)
				NetworkManager.NetworkTickSystem.Tick -= Tick;
		}
		private void Tick()
		{
			if (IsOwner)
			{
				I inputState = CreateInputState();
				ExecuteInput(inputState);
				_bufferedInputStates.Add(inputState);
				_bufferedTransformStates.Add(_currentTransformState);
				
				if (IsHost)
					SetServerTransformStates(_currentTransformState);
				else
					SendInput(_bufferedInputStates.GetListForTicks(INPUT_TICKS_SEND));
			}
			else if (IsServer)
			{
				if (_currentTransformState.Tick < NetworkManager.LocalTime.Tick - 1) // missed a tick
				{
					I inputState = (I)_lastSaveInput.GetStateWithTick(NetworkManager.LocalTime.Tick - 1);
					ExecuteInput(inputState);
					_bufferedTransformStates.Add(_currentTransformState);

					SetUnsafeServerTransformState(_currentTransformState);
				}

				// If _bufferedInputStates contains current tick
				if (_bufferedInputStates.Contains(NetworkManager.LocalTime.Tick))
				{
					I inputState = _bufferedInputStates[NetworkManager.LocalTime.Tick];
					// execute current tick
					ExecuteInput(inputState);

					// update _serverTransformState, _bufferedTransformStates and _lastSaveInput/_lastSaveTransform
					SetServerTransformStates(_currentTransformState);

					_lastSaveInput = inputState;
					_lastSaveTransform = _currentTransformState;
					_bufferedTransformStates.Add(_currentTransformState);
				}
			}
			
			TransformStateChanged?.Invoke(_currentTransformState);
		}
		protected abstract void SetServerTransformStates(T transformState);
		protected abstract void SetUnsafeServerTransformState(T transformState);
		protected abstract void SetSafeServerTransformState(T transformState);
        protected abstract void CorrectState(T transformState);
		protected abstract void SendInput(NetworkStateList<I> inputStates);
		protected void OnInput(NetworkStateList<I> inputStates)
		{
			if (inputStates.LastTick <= _lastSaveInput.Tick)
				return; // Received states to old

			// add all input states after _lastSaveInput to _bufferedInputStates
			_bufferedInputStates.Insert(inputStates, _lastSaveInput.Tick);

			// execute all input states between _lastSaveInput.Tick and current tick/last received tick (reconceliation)
			_currentTransformState = _lastSaveTransform;
			CorrectState(_currentTransformState);
			for (int tick = _lastSaveInput.Tick + 1; tick <= NetworkManager.LocalTime.Tick; tick++)
			{
				ExecuteInput(_bufferedInputStates[tick]);
				// update _bufferedTransformStates
				_bufferedTransformStates.Add(_currentTransformState);
			}

			// update _serverTransformState and _lastSaveInput/_lastSaveTransform
			SetUnsafeServerTransformState(_currentTransformState);
			// _safeServerTransformState is last non-predicted state
			SetSafeServerTransformState(_bufferedTransformStates[Mathf.Min(NetworkManager.LocalTime.Tick, _bufferedInputStates.LastTick)]);

			_lastSaveInput = _bufferedInputStates[Mathf.Max(inputStates.LastTick, _lastSaveInput.Tick)];
			_lastSaveTransform = _bufferedTransformStates[Mathf.Max(inputStates.LastTick, _lastSaveInput.Tick)];
		}
        protected abstract bool ErrorThresholdPassed(T received, T current);
		// Clients
		protected void OnSafeServerStateChanged(T receivedState) // from wrapper
		{
			if (IsOwner && !IsServer)
			{
				T transformState = _bufferedTransformStates[receivedState.Tick] as T;
				if (transformState != null)
				{
					if (ErrorThresholdPassed(receivedState, transformState))
						return;

					Debug.Log("reconceliation tick: " + receivedState.Tick);
					// perform reconceliation
					_currentTransformState = receivedState;
					CorrectState(_currentTransformState);
					for (int tick = receivedState.Tick + 1; tick <= _bufferedInputStates.LastTick; tick++)
					{
						ExecuteInput(_bufferedInputStates[tick]);
						_bufferedTransformStates.Add(_currentTransformState);
					}
				}
				else
					Debug.Log("state received from server to old");
			}
		}
		protected void OnServerStateChanged(T receivedState) // from wrapper
		{
			if (!IsOwner && !IsServer)
			{
				_currentTransformState = receivedState;
				CorrectState(_currentTransformState);
				_bufferedTransformStates.Add(receivedState);
			}
		}
		protected abstract I CreateInputState();
		protected abstract T CreateTransformState();
		protected abstract void ExecuteInput(I inputState);
		public bool GetState(int tick, out T transformState)
		{
			if (tick == NetworkManager.LocalTime.Tick)
			{
				transformState = _currentTransformState;
				return true;
			}
			transformState = _bufferedTransformStates[tick];
			return transformState != null;
		}
	}
}