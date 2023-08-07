using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
	[RequireComponent(typeof(CharacterController))]
	public class FirstPersonController : NetworkBehaviour
	{
		[Header("Player")]
		[SerializeField] private Transform Playermodel;
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

		[Header("Camera")]
		[SerializeField] private Transform CameraSocket;
		[Tooltip("How far in degrees can you move the camera up")]
		[SerializeField] private float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		[SerializeField] private float BottomClamp = -90.0f;

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

		[SerializeField]
		private GlobalInputs GI;

		private InputMaster _controls; // Owneronly
		private CharacterController _controller;

		// Buffer and Interpolation
		private NetworkTransformStateList _bufferedTransformStates;
		private NetworkInputStateList _bufferedInputStates;
		private Dictionary<int, NetworkInputState> _inputsReceived; // Serveronly
		private NetworkInputState _lastReceivedInput; // Serveronly
		private NetworkVariable<NetworkTransformState> _serverTransformState = new NetworkVariable<NetworkTransformState>();
		private InterpolationState _lerpStartInterpolationState;
		private InterpolationState _lerpEndInterpolationState;
		private NetworkTransformState _currentTransformState = new(0);
		private float _currentTickDeltaTime;
		private const int BUFFER_SIZE = 200;
		private const int INPUT_TICKS_SEND = 30;
		private const float MOVEMENT_ERROR_THRESHOLD = 0.03f;
		private const float ROTATION_ERROR_THRESHOLD = 0.02f;

		private void Start()
		{
			_lerpStartInterpolationState = new InterpolationState(Playermodel.position, Vector2.zero);
			_lerpEndInterpolationState = new InterpolationState(Playermodel.position, Vector2.zero);
			// the square root of H * -2 * G = how much velocity needed to reach desired height
			_jumpVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
			_controller = GetComponent<CharacterController>();
			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
			// Subscribe to tick and OnServerStateChanged events
			NetworkManager.NetworkTickSystem.Tick += Tick;
			_serverTransformState.OnValueChanged += OnServerStateChanged;
			// reset Network Variables
			if (IsServer)
			{
				_serverTransformState.Value = new NetworkTransformState(NetworkManager.LocalTime.Tick, Playermodel.position, Vector2.zero, Vector3.zero);
				_inputsReceived = new Dictionary<int, NetworkInputState>();
				_lastReceivedInput = new NetworkInputState(NetworkManager.LocalTime.Tick);
			}
			if (IsOwner)
			{
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
		}
		private void Update()
		{
			_currentTickDeltaTime += Time.deltaTime;
			float relativeDeltaTime = _currentTickDeltaTime / NetworkManager.LocalTime.FixedDeltaTime;

			Vector3 position = Vector3.Lerp(_lerpStartInterpolationState.Position, _lerpEndInterpolationState.Position, relativeDeltaTime);

			// the owner should always have the most accurate lookrotation possible
			Vector2 lookRotation;
			if (IsOwner)
			{
				ReadRotationDelta();
				lookRotation = _lerpStartInterpolationState.LookRotation + _lookDelta;
			}
			else
				lookRotation = Utility.Vector2RotateLerp(_lerpStartInterpolationState.LookRotation, _lerpEndInterpolationState.LookRotation, relativeDeltaTime);
			Transform(position, lookRotation);
		}
		private void Tick()
		{
			if (IsOwner)
			{
				NetworkInputState inputState = CreateInputState();
				CalculateTransformStates(inputState, NetworkManager.LocalTime.Tick);
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
				if (_serverTransformState.Value.Tick < NetworkManager.LocalTime.Tick - 1) // missed a tick
				{
					CalculateTransformStates(_lastReceivedInput, NetworkManager.LocalTime.Tick - 1);
					StoreBuffer(_lastReceivedInput, _currentTransformState);

					_serverTransformState.Value = _currentTransformState;
					_serverTransformState.Value.Predicted = true;
				}
				if (_inputsReceived.ContainsKey(NetworkManager.LocalTime.Tick)) // Applying received input
				{
					CalculateTransformStates(_inputsReceived[NetworkManager.LocalTime.Tick], NetworkManager.LocalTime.Tick);
					StoreBuffer(_inputsReceived[NetworkManager.LocalTime.Tick], _currentTransformState);

					_lastReceivedInput = _inputsReceived[NetworkManager.LocalTime.Tick];
					_inputsReceived.Remove(NetworkManager.LocalTime.Tick);

					_serverTransformState.Value = _currentTransformState;
				}
			}
		}
		private void StoreBuffer(NetworkInputState inputState, NetworkTransformState transformState)
		{
			_bufferedInputStates.Add(inputState);
			_bufferedInputStates.RemoveOutdated();
			
			_bufferedTransformStates.Add(transformState);
			_bufferedTransformStates.RemoveOutdated();
		}
		[ServerRpc]
		private void OnInputServerRpc(NetworkInputStateList inputStates)
		{
			// Input should be executed instantly
			if (inputStates.LastState.Tick == NetworkManager.LocalTime.Tick)
			{
				CalculateTransformStates(inputStates.LastState, NetworkManager.LocalTime.Tick);
				StoreBuffer(inputStates.LastState, _currentTransformState);
				_lastReceivedInput = inputStates.LastState;
				
				_serverTransformState.Value = _currentTransformState;
			}
			else if (inputStates.LastState.Tick > NetworkManager.LocalTime.Tick && !_inputsReceived.ContainsKey(inputStates.LastState.Tick)) // Input should be stashed to be executed later
				_inputsReceived.Add(inputStates.LastState.Tick, inputStates.LastState);
		}
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

					// Rewind logic
					Debug.Log("reconceliation");
					_currentTransformState = receivedState;
					transform.position = receivedState.Position;
					for (int tick = receivedState.Tick + 1; tick <= _bufferedTransformStates.LastState.Tick; tick++)
					{
						CalculateTransformStates(_bufferedInputStates[tick], tick);
						_bufferedTransformStates[tick] = _currentTransformState;
					}
				}
				else
				{
					// Complete desync
					Debug.Log("desync");
				}
			}
			else if (!IsServer)
			{
				_currentTickDeltaTime = 0;
				_lerpStartInterpolationState = new InterpolationState(Playermodel.position, _currentTransformState.LookRotation);
				_lerpEndInterpolationState = new InterpolationState(receivedState.Position, receivedState.LookRotation);
				_currentTransformState = receivedState;
			}
		}
		private NetworkInputState CreateInputState()
		{
			return new NetworkInputState(NetworkManager.NetworkTickSystem.LocalTime.Tick,
				_controls.Player.Move.ReadValue<Vector2>(), _lookDelta,
				_controls.Player.Sprint.ReadValue<float>().AsBool(), _jump);
		}

		private void CalculateTransformStates(NetworkInputState inputState, int tick)
		{
			_currentTickDeltaTime = 0;
			_lerpStartInterpolationState = new InterpolationState(Playermodel.position, _currentTransformState.LookRotation + inputState.LookDelta);

			// lookRotation
			Vector2 lookRotation = _currentTransformState.LookRotation + inputState.LookDelta;

			// clamp our pitch rotation
			lookRotation.x = Mathf.Clamp(lookRotation.x, BottomClamp, TopClamp);
			lookRotation.y = lookRotation.y.ClampToAngle();

			// movement
			Vector3 velocity = CalculateVelocity(inputState, lookRotation);
			Vector3 movement = velocity * NetworkManager.LocalTime.FixedDeltaTime;
			_controller.Move(movement);

			_currentTransformState = new NetworkTransformState(tick, transform.position, lookRotation, velocity);
			_lerpEndInterpolationState = new InterpolationState(_currentTransformState);
		}
		private bool GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = _currentTransformState.Position + Vector3.down * GroundedOffset;
			return Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
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
			float currentHorizontalSpeed = _currentTransformState.Velocity.WithHeight(0).magnitude;

			float speed;
			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - SPEED_OFFSET || currentHorizontalSpeed > targetSpeed + SPEED_OFFSET)
			{
				float speedChangeRate = grounded ? GroundSpeedChangeRate : AirSpeedChangeRate;
				speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, NetworkManager.LocalTime.FixedDeltaTime * speedChangeRate);
				speed = Utility.Round(speed, 0.001f);
			}
			else
				speed = targetSpeed;

			Vector3 input = inputState.MovementInput.Rotate(lookRotation.y).AddHeight(0).normalized * speed;

			return input + Vector3.up * verticalVelocity;
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
			
			return verticalVelocity;
		}
		private void Transform(Vector3 position, Vector2 lookRotation)
		{
			// Update camera target pitch
			CameraSocket.localRotation = Quaternion.Euler(lookRotation.x, 0.0f, 0.0f);

			// rotate the player left and right
			Playermodel.rotation = Quaternion.Euler(0, lookRotation.y, 0);

			// move playermodel
			Playermodel.position = position;
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
		private struct InterpolationState
		{
			public readonly Vector3 Position;
			public readonly Vector2 LookRotation;
			public InterpolationState(Vector3 position, Vector2 lookRotation)
			{
				Position = position;
				LookRotation = lookRotation;
			}
			public InterpolationState(NetworkTransformState state)
			{
				Position = state.Position;
				LookRotation = state.LookRotation;
			}
		}
	}
}