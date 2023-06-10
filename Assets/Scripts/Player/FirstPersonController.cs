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
		[Header("Interpolation")]
		[Tooltip("The maximum amout the player can move horizontally each second. must be higher than the Sprint speed")]
		[SerializeField] private float MaxHorizontalMovement = 15;

		// player
		private float _jumpVelocity;
		private bool _jump; // Owner only
		private Vector2 _lookRotation;
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
		private List<BufferedState> _bufferedStates;
		private Dictionary<int, NetworkInputState> _inputsReceived; // Serveronly
		private NetworkInputState _lastReceivedInput; // Serveronly
		private NetworkVariable<NetworkTransformState> _serverTransformState = new NetworkVariable<NetworkTransformState>();
		private InterpolationState _lerpStartInterpolationState;
		private InterpolationState _lerpEndInterpolationState;
		private NetworkTransformState _currentTransformState = new(0);
		private float _currentTickDeltaTime;
		private bool _correcting;
		private const int BUFFER_SIZE = 200;
		private const float ERROR_THRESHOLD = 0.03f;

		private void Start()
		{
			_lerpStartInterpolationState = new InterpolationState(Playermodel.position, _lookRotation);
			_lerpEndInterpolationState = new InterpolationState(Playermodel.position, _lookRotation);
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
				_serverTransformState.Value = CreateTransformState();
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
				_bufferedStates = new List<BufferedState>();
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

			Vector3 position = Vector3.LerpUnclamped(_lerpStartInterpolationState.Position, _lerpEndInterpolationState.Position, relativeDeltaTime);

			// the owner should always have the most accurate lookrotation possible
			if (IsOwner)
				CalculateRotation();
			else
				_lookRotation = Utility.Vector2RotateLerpUnclamped(_lerpStartInterpolationState.LookRotation, _lerpEndInterpolationState.LookRotation, relativeDeltaTime);
			Transform(position, _lookRotation);
		}
		private void Tick()
		{
			if (IsOwner)
			{
				NetworkInputState inputState = CreateInputState();
				CalculateTransformStates(inputState, NetworkManager.LocalTime.Tick);
				if (_correcting)
					_lerpEndInterpolationState = GetEndInterpolationState();
				else
					_lerpEndInterpolationState = new InterpolationState(_currentTransformState);
				StoreBuffer(NetworkManager.LocalTime.Tick, inputState, _currentTransformState);
				
				if (IsHost)
					_serverTransformState.Value = _currentTransformState;
				else
					OnInputServerRpc(inputState);
				
				if (_jump)
					_jump = false;
			}
			else if (IsServer)
			{
				if (_serverTransformState.Value.Tick < NetworkManager.LocalTime.Tick - 1) // missed a tick
				{
					CalculateTransformStates(_lastReceivedInput, NetworkManager.LocalTime.Tick - 1);
					_lerpEndInterpolationState = new InterpolationState(_currentTransformState);
					StoreBuffer(NetworkManager.LocalTime.Tick - 1, _lastReceivedInput, _currentTransformState);

					_serverTransformState.Value = _currentTransformState;
				}
				if (_inputsReceived.ContainsKey(NetworkManager.LocalTime.Tick)) // Applying received input
				{
					CalculateTransformStates(_inputsReceived[NetworkManager.LocalTime.Tick], NetworkManager.LocalTime.Tick);
					_lerpEndInterpolationState = new InterpolationState(_currentTransformState);
					StoreBuffer(NetworkManager.LocalTime.Tick, _inputsReceived[NetworkManager.LocalTime.Tick], _currentTransformState);

					_lastReceivedInput = _inputsReceived[NetworkManager.LocalTime.Tick];
					_inputsReceived.Remove(NetworkManager.LocalTime.Tick);

					_serverTransformState.Value = _currentTransformState;
				}
			}
		}
		private void StoreBuffer(int tick, NetworkInputState inputState, NetworkTransformState transformState)
		{
			_bufferedStates.Add(new(tick, inputState, transformState));
			if (_bufferedStates.Count >= BUFFER_SIZE)
				_bufferedStates.RemoveAt(0);
		}
		[ServerRpc]
		private void OnInputServerRpc(NetworkInputState inputState)
		{
			// Input should be executed instantly
			if (inputState.Tick == NetworkManager.LocalTime.Tick)
			{
				CalculateTransformStates(inputState, NetworkManager.LocalTime.Tick);
				_lerpEndInterpolationState = new InterpolationState(_currentTransformState);
				StoreBuffer(NetworkManager.LocalTime.Tick, inputState, _currentTransformState);
				_lastReceivedInput = inputState;
				
				_serverTransformState.Value = _currentTransformState;
			}
			else if (inputState.Tick > NetworkManager.LocalTime.Tick) // Input should be stashed to be executed later
				_inputsReceived.Add(inputState.Tick, inputState);
		}
		private void OnServerStateChanged(NetworkTransformState previouseState, NetworkTransformState receivedState)
		{
			if (IsOwner && !IsServer)
			{
				if (StateBuffered(receivedState.Tick, out int stateId))
				{
					if ((_bufferedStates[stateId].TransformState.Position - receivedState.Position).sqrMagnitude <= ERROR_THRESHOLD)
						return;

					// Rewind logic
					Debug.Log("reconceliation");
					_currentTransformState = receivedState;
					transform.position = receivedState.Position;
					for (int i = stateId + 1; i < _bufferedStates.Count; i++)
					{
						CalculateTransformStates(_bufferedStates[i].InputState, _bufferedStates[i].Tick);
						_bufferedStates[i].TransformState = _currentTransformState;
					}
					_correcting = true;
					_lerpEndInterpolationState = GetEndInterpolationState();
					// Delete outdated data
					for (int i = stateId - 1; i >= 0; i--)
						_bufferedStates.RemoveAt(0);
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
				_lerpStartInterpolationState = new InterpolationState(Playermodel.position, _lookRotation);
				_lerpEndInterpolationState = new InterpolationState(receivedState.Position, receivedState.LookRotation);
				_currentTransformState = receivedState;
			}
		}
		private NetworkInputState CreateInputState()
		{
			return new NetworkInputState(NetworkManager.NetworkTickSystem.LocalTime.Tick,
				_controls.Player.Move.ReadValue<Vector2>(), _lookRotation,
				_controls.Player.Sprint.ReadValue<float>().AsBool(), _jump);
		}
		private NetworkTransformState CreateTransformState() =>
			new NetworkTransformState(NetworkManager.LocalTime.Tick, Playermodel.position, _lookRotation, _currentTransformState.Velocity);

		private void CalculateTransformStates(NetworkInputState inputState, int tick)
		{
			_currentTickDeltaTime = 0;
			_lerpStartInterpolationState = new InterpolationState(Playermodel.position, _lookRotation);

			Vector3 velocity = CalculateVelocity(inputState);
			Vector3 movement = velocity * NetworkManager.LocalTime.FixedDeltaTime;
			_controller.Move(movement);
			_currentTransformState = new NetworkTransformState(tick, transform.position, inputState.LookRotation, velocity);
		}

		private bool GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = _currentTransformState.Position + Vector3.down * GroundedOffset;
			return Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}
		private void CalculateRotation()
		{
			Vector2 lookInput = _controls.Mouse.Look.ReadValue<Vector2>();

			// Camera rotation
			_lookRotation.x += lookInput.y * RotationSpeed;
			float _rotationVelocity = lookInput.x * RotationSpeed;

			// clamp our pitch rotation
			_lookRotation.x = Mathf.Clamp(_lookRotation.x, BottomClamp, TopClamp);

			_lookRotation.y += _rotationVelocity;
			_lookRotation.y = _lookRotation.y.ClampToAngle();
		}
		private Vector3 CalculateVelocity(NetworkInputState inputState)
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

			Vector3 input = inputState.MovementInput.Rotate(inputState.LookRotation.y).AddHeight(0).normalized * speed;

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
			bool stateBuffered = StateBuffered(tick, out int stateId);
			transformState = stateBuffered ? _bufferedStates[stateId].TransformState : null;
			return stateBuffered;
		}
		private bool StateBuffered(int tick, out int stateId)
		{
			for (int i = _bufferedStates.Count - 1; i >= 0; i--)
			{
				if (_bufferedStates[i].Tick == tick)
				{
					stateId = i;
					return true;
				}
			}
			stateId = 0;
			return false;
		}
		private InterpolationState GetEndInterpolationState()
		{
			Vector3 movement = _currentTransformState.Position - _lerpStartInterpolationState.Position;
			Vector3 horizontalMovement = movement.XZ();
			if (horizontalMovement.magnitude > MaxHorizontalMovement * NetworkManager.LocalTime.FixedDeltaTime)
			{
				Debug.Log("bigError");
				Vector2 newMovement = horizontalMovement.normalized * MaxHorizontalMovement * NetworkManager.LocalTime.FixedDeltaTime;
				return new InterpolationState(_lerpStartInterpolationState.Position + newMovement.AddHeight(movement.y), _currentTransformState.LookRotation);
			}
			else
			{
				_correcting = false;
				return new InterpolationState(_currentTransformState);
			}

		}
		private class BufferedState
		{
			public readonly int Tick;
			public NetworkTransformState TransformState;
			public readonly NetworkInputState InputState;

			public BufferedState(int tick, NetworkInputState inputState, NetworkTransformState transformState)
			{
				Tick = tick;
				TransformState = transformState;
				InputState = inputState;
			}
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