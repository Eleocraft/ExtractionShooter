using UnityEngine;
using Unity.Netcode;
using System;

namespace ExoplanetStudios.ExtractionShooter
{
	[RequireComponent(typeof(CharacterController))]
	public class FirstPersonController : NetworkBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		[SerializeField] private float MoveSpeed = 4.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		[SerializeField] private float SprintSpeed = 6.0f;
		[Tooltip("Rotation speed of the character")]
		[SerializeField] private float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		[SerializeField] private float GroundSpeedChangeRate = 8.0f;
		[SerializeField] private float AirSpeedChangeRate = 3.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		[SerializeField] private float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		[SerializeField] private float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		[SerializeField] private float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		[SerializeField] private float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("Useful for rough ground")]
		[SerializeField] private float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		[SerializeField] private float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		[SerializeField] private LayerMask GroundLayers;
		
		[Header("Player Headblock")]
		[SerializeField] private float HeadblockOffset = 0.5f;

		[Header("Camera")]
		[SerializeField] private Transform CameraSocket;
		[Tooltip("How far in degrees can you move the camera up")]
		[SerializeField] private float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		[SerializeField] private float BottomClamp = -90.0f;

		[Header("Inputs")]
		[SerializeField] private GlobalInputs GI;
		[Header("Test")]
		[SerializeField] private bool Walk;

		public Action<NetworkTransformState> TransformStateChanged;

		// player
		private float _jumpVelocity;
		private bool _jump; // Owner only
		private Vector2 _lookDelta; // Owner only
		private const float TERMINAL_VELOCITY = 100.0f;
		private const string CAMERA_POS_NAME = "CameraPos";
		private const string PLAYER_CAM_TAG = "PlayerCam";

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;
		private const float SPEED_OFFSET = 0.05f;

		private InputMaster _controls; // Owneronly
		private CharacterController _controller;

		// Buffer
		private NetworkTransformStateList _bufferedTransformStates; // Owner and Server
		private NetworkInputStateList _bufferedInputStates; // Owner and Server
		
		private NetworkVariable<NetworkTransformState> _serverTransformState = new NetworkVariable<NetworkTransformState>();
		private NetworkTransformState _currentTransformState = new(0); // The current transform state

		private NetworkInputState _lastSaveInput; // Serveronly
		private NetworkTransformState _lastSaveTransform; // Serveronly

		// Interpolation
		private PlayerInterpolation _interpolation;

		// Constants
		private const int BUFFER_SIZE = 200;
		private const int INPUT_TICKS_SEND = 30;
		private const float MOVEMENT_ERROR_THRESHOLD = 0.03f;
		private const float ROTATION_ERROR_THRESHOLD = 0.02f;

		public override void OnNetworkSpawn()
		{
			// the square root of H * -2 * G = how much velocity needed to reach desired height
			_jumpVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
			_controller = GetComponent<CharacterController>();
			_interpolation = GetComponent<PlayerInterpolation>();
			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
			// Subscribe to tick and OnServerStateChanged events
			NetworkManager.NetworkTickSystem.Tick += Tick;
			_serverTransformState.OnValueChanged += OnServerStateChanged;
			// reset Network Variables
			if (IsServer)
			{
				_serverTransformState.Value = new NetworkTransformState(NetworkManager.LocalTime.Tick, transform.position, Vector2.zero, Vector3.zero);
				_lastSaveInput = new NetworkInputState(NetworkManager.LocalTime.Tick);
				_lastSaveTransform = new NetworkTransformState(NetworkManager.LocalTime.Tick);
			}
			if (IsOwner)
			{
				_interpolation.SetOwner();
				GameObject.FindGameObjectWithTag(PLAYER_CAM_TAG).GetComponent<Cinemachine.CinemachineVirtualCamera>().Follow = CameraSocket.Find(CAMERA_POS_NAME);
				_controls = GI.Controls;
				_controls.Player.Jump.started += JumpInput;
			}
			if (IsServer || IsOwner)
			{
				_bufferedTransformStates = new(BUFFER_SIZE);
				_bufferedInputStates = new(BUFFER_SIZE);
			}
		}
		public override void OnDestroy()
		{
			base.OnDestroy();

			if (NetworkManager?.NetworkTickSystem != null)
				NetworkManager.NetworkTickSystem.Tick -= Tick;
			
			_serverTransformState.OnValueChanged -= OnServerStateChanged;

			if (IsOwner)
				_controls.Player.Jump.started -= JumpInput;
		}

		private void JumpInput(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => _jump = true;
		public void SetPosition(Vector3 position)
		{
			if (!IsServer) return;

			transform.position = position;
			_currentTransformState = CreateTransformState();
			_serverTransformState.Value = _currentTransformState;
			_lastSaveTransform = _currentTransformState;
		}
		private void Tick()
		{
			if (IsOwner)
			{
				NetworkInputState inputState = CreateInputState();
				ExecuteInput(inputState);
				StoreBuffer(inputState, _currentTransformState);
				
				if (IsHost)
					_serverTransformState.Value = _currentTransformState;
				else
					OnInputServerRpc(_bufferedInputStates.GetListForTicks(INPUT_TICKS_SEND));
				
				if (_jump)
					_jump = false;

				_lookDelta = Vector2.zero;
			}
			else if (IsServer)
			{
				if (_currentTransformState.Tick < NetworkManager.LocalTime.Tick - 1) // missed a tick
				{
					NetworkInputState inputState = new NetworkInputState(_lastSaveInput, NetworkManager.LocalTime.Tick - 1);
					ExecuteInput(inputState);
					StoreBuffer(inputState, _currentTransformState);

					_serverTransformState.Value = _currentTransformState;
					
					if (NetworkManager.LocalTime.Tick - _lastSaveInput.Tick < INPUT_TICKS_SEND) // might be corrected in next package
						_serverTransformState.Value.Predicted = true;
				}

				// If _bufferedInputStates contains current tick
				if (_bufferedInputStates.Contains(NetworkManager.LocalTime.Tick))
				{
					NetworkInputState input = _bufferedInputStates[NetworkManager.LocalTime.Tick];
					// execute current tick
					ExecuteInput(input);

					// update _serverTransformState, _bufferedTransformStates and _lastSaveInput/_lastSaveTransform
					_serverTransformState.Value = _currentTransformState;

					_lastSaveInput = input;
					_lastSaveTransform = _currentTransformState;
					_bufferedTransformStates.Add(_currentTransformState);
				}
			}
			TransformStateChanged?.Invoke(_currentTransformState);
		}
		[ServerRpc]
		private void OnInputServerRpc(NetworkInputStateList inputStates)
		{
			if (inputStates.LastTick <= _lastSaveInput.Tick)
				return; // Received states to old

			// add all input states after _lastSaveInput to _bufferedInputStates
			_bufferedInputStates.Insert(inputStates, _lastSaveInput.Tick);

			// execute all input states between _lastSaveInput.Tick and current tick/last received tick (reconceliation)
			_currentTransformState = _lastSaveTransform;
			transform.position = _lastSaveTransform.Position;
			int lastTickToExecute = Mathf.Min(NetworkManager.LocalTime.Tick, _bufferedInputStates.LastTick);
			for (int tick = _lastSaveInput.Tick + 1; tick <= lastTickToExecute; tick++)
			{
				ExecuteInput(_bufferedInputStates[tick]);
				// update _bufferedTransformStates
				_bufferedTransformStates.Add(_currentTransformState);
			}

			// update _serverTransformState and _lastSaveInput/_lastSaveTransform
			_serverTransformState.Value = _currentTransformState;

			_lastSaveInput = _bufferedInputStates[inputStates.LastTick];
			_lastSaveTransform = _currentTransformState;
		}
		private void StoreBuffer(NetworkInputState inputState, NetworkTransformState transformState)
		{
			_bufferedInputStates.Add(inputState);
			_bufferedTransformStates.Add(transformState);
		}
		// Clients
		private void OnServerStateChanged(NetworkTransformState previouseState, NetworkTransformState receivedState)
		{
			if (IsOwner && !IsServer)
			{
				if (receivedState.Predicted)
					return;
				
				NetworkTransformState transformState = _bufferedTransformStates[receivedState.Tick];
				if (transformState != null)
				{
					if ((transformState.Position - receivedState.Position).sqrMagnitude <= MOVEMENT_ERROR_THRESHOLD
						&& (transformState.LookRotation - receivedState.LookRotation).sqrMagnitude <= ROTATION_ERROR_THRESHOLD)
						return;

					Debug.Log("reconceliation tick: " + receivedState.Tick);
					// perform reconceliation
					_currentTransformState = receivedState;
					transform.position = receivedState.Position;
					for (int tick = receivedState.Tick + 1; tick <= _bufferedInputStates.LastTick; tick++)
					{
						ExecuteInput(_bufferedInputStates[tick]);
						_bufferedTransformStates.Add(_currentTransformState);
					}
				}
				else
					Debug.Log("state received from server to old");
			}
			else if (!IsServer)
			{
				_currentTransformState = receivedState;
				transform.position = _currentTransformState.Position;
				_interpolation.SetInterpolationStates(previouseState.LookRotation, _currentTransformState);
			}
		}
		private NetworkInputState CreateInputState()
		{
			return new NetworkInputState(NetworkManager.LocalTime.Tick,
				Walk ? Vector2.up : _controls.Player.Move.ReadValue<Vector2>(), _lookDelta,
				_controls.Player.Sprint.ReadValue<float>().AsBool(), _jump);
		}
		private NetworkTransformState CreateTransformState()
		{
			return new NetworkTransformState(NetworkManager.LocalTime.Tick,
				transform.position, Vector3.forward, Vector3.zero);
		}
		private void ExecuteInput(NetworkInputState inputState)
		{
			if (inputState == null)
				return;
			
			// lookRotation
			Vector2 lookRotation = _currentTransformState.LookRotation + inputState.LookDelta;

			// Start interpolation state
			_interpolation.SetStartInterpolationState(lookRotation);

			// clamp our pitch rotation
			lookRotation.x = Mathf.Clamp(lookRotation.x, BottomClamp, TopClamp);
			lookRotation.y = lookRotation.y.ClampToAngle();

			// movement
			Vector3 velocity = CalculateVelocity(inputState, lookRotation);
			Vector3 movement = velocity * NetworkManager.LocalTime.FixedDeltaTime;
			_controller.Move(movement);

			_currentTransformState = new NetworkTransformState(inputState.Tick, transform.position, lookRotation, velocity);

			// End interpolation state
			_interpolation.SetEndInterpolationState(_currentTransformState);
		}
		private bool GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = _currentTransformState.Position + Vector3.up * GroundedOffset;
			return Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}
		private bool HeadblockCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = _currentTransformState.Position + Vector3.up * HeadblockOffset;
			return Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}
		private void Update()
		{
			if (IsOwner)
			{
				// if this is the owner the lookrotation should be calculated locally
				ReadRotationDelta();
				Vector2 lookRotation = GetLocalLookRotation();

				// Update camera target pitch
				CameraSocket.localRotation = Quaternion.Euler(lookRotation.x, 0.0f, 0.0f);

				// rotate the player left and right
				transform.rotation = Quaternion.Euler(0, lookRotation.y, 0);
			}
		}
		private void ReadRotationDelta()
		{
			Vector2 lookInput = _controls.Mouse.Look.ReadValue<Vector2>();

			// Camera rotation
			_lookDelta.x += lookInput.y * RotationSpeed;
			_lookDelta.y += lookInput.x * RotationSpeed;
		}
		private Vector2 GetLocalLookRotation() => _currentTransformState.LookRotation + _lookDelta;
		private Vector3 CalculateVelocity(NetworkInputState inputState, Vector2 lookRotation)
		{
			bool grounded = GroundedCheck();
			float verticalVelocity = CalculateGravity(inputState.Jump, grounded);
			
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = inputState.Sprint ? SprintSpeed : MoveSpeed;

			// target speed is 0 if no key is pressed
			if (inputState.MovementInput == Vector2.zero) targetSpeed = 0.0f;

			Vector2 targetHorizontalVelocity = inputState.MovementInput.Rotate(lookRotation.y) * targetSpeed;
			Vector2 horizontalVelocity = _currentTransformState.Velocity.XZ();

			// accelerate or decelerate to target speed
			if ((horizontalVelocity - targetHorizontalVelocity).sqrMagnitude > SPEED_OFFSET)
			{
				float speedChangeRate = grounded ? GroundSpeedChangeRate : AirSpeedChangeRate;
				horizontalVelocity = Vector2.Lerp(horizontalVelocity, targetHorizontalVelocity, NetworkManager.LocalTime.FixedDeltaTime * speedChangeRate);
			}
			else
				horizontalVelocity = targetHorizontalVelocity;

			return horizontalVelocity.AddHeight(verticalVelocity);
		}

		private float CalculateGravity(bool jump, bool grounded)
		{
			float verticalVelocity = _currentTransformState.Velocity.y;
			if (grounded)
			{
				if (jump && _jumpTimeoutDelta <= 0.0f) // if jump
					verticalVelocity = _jumpVelocity;
				else
				{
					// reset the fall timeout timer
					_fallTimeoutDelta = FallTimeout;

					// stop our velocity dropping infinitely when grounded
					if (verticalVelocity < 0.0f)
						verticalVelocity = -2f;

					// jump timeout
					if (_jumpTimeoutDelta >= 0.0f)
						_jumpTimeoutDelta -= NetworkManager.LocalTime.FixedDeltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
					_fallTimeoutDelta -= NetworkManager.LocalTime.FixedDeltaTime;
			}

			// apply gravity over time if under terminal
			if (verticalVelocity < TERMINAL_VELOCITY)
				verticalVelocity += Gravity * NetworkManager.LocalTime.FixedDeltaTime;
			
			if (HeadblockCheck() && verticalVelocity > 0)
				verticalVelocity = 0;
			
			return verticalVelocity;
		}
		public bool GetState(int tick, out NetworkTransformState transformState)
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