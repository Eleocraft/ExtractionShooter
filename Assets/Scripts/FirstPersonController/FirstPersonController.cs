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
		private Vector2 _lookRotation;
		private const float TERMINAL_VELOCITY = 100.0f;
		private const string CAMERA_SOCKET_NAME = "CameraSocket";
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
		private List<BufferedState> _bufferedStates; // Owneronly
		private Dictionary<int, NetworkInputState> _inputsReceived; // Serveronly
		private NetworkInputState _lastReceivedInput; // Serveronly
		private NetworkVariable<NetworkTransformState> _serverTransformState = new NetworkVariable<NetworkTransformState>();
		private NetworkTransformState _lerpStartTransformState = new(0);
		private NetworkTransformState _currentTransformState = new(0);
		private float _currentTickDeltaTime;
		private const int BUFFER_SIZE = 200;
		private const float ERROR_THRESHOLD = 0.03f;

		private void Start()
		{
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
				GameObject.FindGameObjectWithTag(PLAYER_CAM_TAG).GetComponent<Cinemachine.CinemachineVirtualCamera>().Follow = CameraSocket;
				_controls = GI.Controls;
				_controls.Player.Jump.started += JumpInput;
				_bufferedStates = new List<BufferedState>();
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
		private void Update()
		{
			_currentTickDeltaTime += Time.deltaTime;
			float relativeDeltaTime = _currentTickDeltaTime / NetworkManager.LocalTime.FixedDeltaTime;

			Vector3 position = Vector3.LerpUnclamped(_lerpStartTransformState.Position, _currentTransformState.Position, relativeDeltaTime);
			// the owner should always have the most accurate lookrotation possible
			if (IsOwner)
				CalculateRotation();
			else
				_lookRotation = Utility.Vector2RotateLerpUnclamped(_lerpStartTransformState.LookRotation, _currentTransformState.LookRotation, relativeDeltaTime);
			Transform(position, _lookRotation);
		}
		private void Tick()
		{
			if (IsOwner)
			{
				NetworkInputState inputState = CreateInputState();
				CalculateTransformStates(inputState);
				
				_bufferedStates.Add(new(NetworkManager.LocalTime.Tick, inputState, _currentTransformState));
				if (_bufferedStates.Count >= BUFFER_SIZE)
					_bufferedStates.RemoveAt(0);
				
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

					_serverTransformState.Value = _currentTransformState;
				}
				if (_inputsReceived.ContainsKey(NetworkManager.LocalTime.Tick)) // Applying received input
				{
					CalculateTransformStates(_inputsReceived[NetworkManager.LocalTime.Tick]);
					_lastReceivedInput = _inputsReceived[NetworkManager.LocalTime.Tick];
					_inputsReceived.Remove(NetworkManager.LocalTime.Tick);

					_serverTransformState.Value = _currentTransformState;
				}
			}
		}
		[ServerRpc]
		private void OnInputServerRpc(NetworkInputState inputState)
		{
			// Input should be executed instantly
			if (inputState.Tick == NetworkManager.LocalTime.Tick)
			{
				CalculateTransformStates(inputState);
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
					_controller.Move(receivedState.Position - transform.position);
					for (int i = stateId + 1; i < _bufferedStates.Count; i++)
					{
						CalculateTransformStates(_bufferedStates[i].InputState, _bufferedStates[i].Tick);
						_bufferedStates[i].TransformState = _currentTransformState;
					}
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
				_lerpStartTransformState = CreateTransformState();
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
			new NetworkTransformState(NetworkManager.LocalTime.Tick, Playermodel.position, _lookRotation, _currentTransformState.CurrentHorizontalSpeed, _currentTransformState.VerticalVelocity);

		private void CalculateTransformStates(NetworkInputState inputState) => 
			CalculateTransformStates(inputState, NetworkManager.LocalTime.Tick);
		private void CalculateTransformStates(NetworkInputState inputState, int tick)
		{
			_currentTickDeltaTime = 0;
			_lerpStartTransformState = CreateTransformState();

			MovementUpdate results = CalculateMovement(inputState);
			_controller.Move(results.Movement);
			_currentTransformState = new NetworkTransformState(tick, transform.position, inputState.LookRotation, results.Speed, results.VerticalVelocity);
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
		private MovementUpdate CalculateMovement(NetworkInputState inputState)
		{
			bool grounded = GroundedCheck();
			float verticalVelocity = CalculateGravity(inputState.Jump, grounded);
			
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = inputState.Sprint ? SprintSpeed : MoveSpeed;

			// target speed is 0 if no key is pressed
			if (inputState.MovementInput == Vector2.zero) targetSpeed = 0.0f;

			float speed;
			// accelerate or decelerate to target speed
			if (_currentTransformState.CurrentHorizontalSpeed < targetSpeed - SPEED_OFFSET || _currentTransformState.CurrentHorizontalSpeed > targetSpeed + SPEED_OFFSET)
			{
				float speedChangeRate = grounded ? GroundSpeedChangeRate : AirSpeedChangeRate;
				speed = Mathf.Lerp(_currentTransformState.CurrentHorizontalSpeed, targetSpeed, NetworkManager.LocalTime.FixedDeltaTime * speedChangeRate);
				speed = Utility.Round(speed, 0.001f);
			}
			else
				speed = targetSpeed;

			Vector3 input = inputState.MovementInput.Rotate(inputState.LookRotation.y).AddHeight(0).normalized * speed;

			return new(input * NetworkManager.LocalTime.FixedDeltaTime + new Vector3(0.0f, verticalVelocity, 0.0f) * NetworkManager.LocalTime.FixedDeltaTime, input.magnitude, verticalVelocity);
		}

		private float CalculateGravity(bool jump, bool grounded)
		{
			float verticalVelocity = _currentTransformState.VerticalVelocity;
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
		private bool StateBuffered(int Tick, out int stateId)
		{
			for (int i = _bufferedStates.Count - 1; i >= 0; i--)
			{
				if (_bufferedStates[i].Tick == Tick)
				{
					stateId = i;
					return true;
				}
			}
			stateId = 0;
			return false;
		}
		private class MovementUpdate
		{
			public Vector3 Movement;
			public float Speed;
			public float VerticalVelocity;
			public MovementUpdate(Vector3 movement, float speed, float verticalVelocity)
			{
				Movement = movement;
				Speed = speed;
				VerticalVelocity = verticalVelocity;
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
	}
}