using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

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

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		[SerializeField] private GameObject CameraSocket;
		[Tooltip("How far in degrees can you move the camera up")]
		[SerializeField] private float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		[SerializeField] private float BottomClamp = -90.0f;

		// player
		private float _speed;
		private float _verticalVelocity;
		private float _speedChangeRate;
		private float _jumpVelocity;
		private Vector2 _lookRotation; // owner only
		private const float TERMINAL_VELOCITY = 100.0f;

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
		private NetworkTransformState _lastTransformState = new();
		private NetworkTransformState _currentTransformState = new();
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
				_lastReceivedInput = new NetworkInputState() { Tick = NetworkManager.LocalTime.Tick };
			}

			if (IsOwner)
			{
				GameObject.FindGameObjectWithTag("PlayerCam").GetComponent<Cinemachine.CinemachineVirtualCamera>().Follow = CameraSocket.transform;
				_controls = GI.Controls;
				_controls.Player.Jump.started += JumpInput;
				_bufferedStates = new List<BufferedState>();
			}
		}
		public override void OnDestroy()
		{
			base.OnDestroy();
			
			_serverTransformState.OnValueChanged -= OnServerStateChanged;

			if (NetworkManager.Singleton != null)
				NetworkManager.NetworkTickSystem.Tick -= Tick;

			if (IsOwner)
				_controls.Player.Jump.started -= JumpInput;
		}

		private void JumpInput(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
		{
			Jump();
			if (!IsHost)
				JumpServerRpc();
		}
		[ServerRpc]
		private void JumpServerRpc() => Jump();
		private void Jump()
		{
			// Jump
			if (GroundedCheck() && _jumpTimeoutDelta <= 0.0f)
				_verticalVelocity = _jumpVelocity; 
		}
		private void Update()
		{
			if (IsOwner)
				CalculateRotation();
			
			_currentTickDeltaTime += Time.deltaTime;
			float relativeDeltaTime = _currentTickDeltaTime / NetworkManager.LocalTime.FixedDeltaTime;

			Vector3 position = Vector3.Lerp(_lastTransformState.Position, _currentTransformState.Position, relativeDeltaTime);
			// the owner should always have the most accurate lookrotation possible
			Vector2 lookRotation = IsOwner ? _lookRotation : _currentTransformState.LookRotation;//Vector2.Lerp(_lastTransformState.LookRotation, _currentTransformState.LookRotation, relativeDeltaTime);
			Transform(position, lookRotation);
		}
		private void Tick()
		{
			if (IsOwner)
			{
				NetworkInputState inputState = CreateInputState();
				CalculateTransformState(inputState);
				
				_bufferedStates.Add(new(NetworkManager.LocalTime.Tick, inputState, _currentTransformState));
				if (_bufferedStates.Count >= BUFFER_SIZE)
					_bufferedStates.RemoveAt(0);
				
				if (IsServer)
					_serverTransformState.Value = _currentTransformState;
				else
					OnInputServerRpc(inputState);
			}
			else if (IsServer)
			{
				if (_serverTransformState.Value.Tick < NetworkManager.LocalTime.Tick - 1) // missed a tick
				{
					CalculateTransformState(_lastReceivedInput);

					_serverTransformState.Value = _currentTransformState;
				}
				if (_inputsReceived.ContainsKey(NetworkManager.LocalTime.Tick)) // Applying received input
				{
					CalculateTransformState(_inputsReceived[NetworkManager.LocalTime.Tick]);
					_lastReceivedInput = _inputsReceived[NetworkManager.LocalTime.Tick];
					_inputsReceived.Remove(NetworkManager.LocalTime.Tick);

					_serverTransformState.Value = _currentTransformState;
				}
			}
		}
		[ServerRpc]
		private void OnInputServerRpc(NetworkInputState inputState)
		{
			Debug.Log(inputState.Tick + "  " + NetworkManager.LocalTime.Tick);

			// Input should be executed instantly
			if (inputState.Tick == NetworkManager.LocalTime.Tick)
			{
				CalculateTransformState(inputState);
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
				if (StateBuffered(receivedState.Tick, out BufferedState state))
				{
					if ((state.TransformState.Position - receivedState.Position).sqrMagnitude > ERROR_THRESHOLD)
					{
						// Rewind logic
						Debug.Log("error found");
						Debug.Log((state.TransformState.Position - receivedState.Position).magnitude);
					}
				}
				else
				{
					// Complete desync
					Debug.Log("desync");
				}
			}
			if (!IsOwner && !IsServer)
			{
				_currentTickDeltaTime = 0;
				_lastTransformState = CreateTransformState();
				_currentTransformState = receivedState;
			}
		}
		private NetworkInputState CreateInputState()
		{
			return new NetworkInputState()
			{
				Tick = NetworkManager.NetworkTickSystem.LocalTime.Tick,
				MovementInput = _controls.Player.Move.ReadValue<Vector2>(),
				Sprint = _controls.Player.Sprint.ReadValue<float>().AsBool(),
				LookRotation = _lookRotation
			};
		}
		private NetworkTransformState CreateTransformState(Vector3 position, Vector2 lookRotation)
		{
			return new NetworkTransformState()
			{
				Tick = NetworkManager.LocalTime.Tick,
				Position = position,
				LookRotation = lookRotation
			};
		}
		private NetworkTransformState CreateTransformState()
		{
			return new NetworkTransformState()
			{
				Tick = NetworkManager.LocalTime.Tick,
				Position = transform.position,
				LookRotation = _lookRotation
			};
		}
		private void CalculateTransformState(NetworkInputState inputState)
		{
			_currentTickDeltaTime = 0;

			Vector3 nextPosition = CalculateMovement(inputState.MovementInput, inputState.LookRotation, inputState.Sprint);
			_currentTransformState = CreateTransformState(nextPosition, inputState.LookRotation);
			_lastTransformState = CreateTransformState();
		}

		private bool GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
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
		private Vector3 CalculateMovement(Vector2 moveInput, Vector2 lookRotation, bool sprint)
		{
			CalculateGravity();

			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = sprint ? SprintSpeed : MoveSpeed;

			// target speed is 0 if no key is pressed
			if (moveInput == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - SPEED_OFFSET || currentHorizontalSpeed > targetSpeed + SPEED_OFFSET)
			{
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, NetworkManager.LocalTime.FixedDeltaTime * _speedChangeRate);
				_speed = Utility.Round(_speed, 0.001f);
			}
			else
				_speed = targetSpeed;

			Vector3 inputDirection = moveInput.Rotate(lookRotation.y).AddHeight(0).normalized;

			// move the player
			// ---> TARGET SPEED FIX
			return transform.position + inputDirection * (targetSpeed * NetworkManager.LocalTime.FixedDeltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * NetworkManager.LocalTime.FixedDeltaTime;
		}

		private void CalculateGravity()
		{
			if (GroundedCheck())
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// SpeedChangeRate
				_speedChangeRate = GroundSpeedChangeRate;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
					_verticalVelocity = -2f;

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
					_jumpTimeoutDelta -= NetworkManager.LocalTime.FixedDeltaTime;
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// SpeedChangeRate
				_speedChangeRate = AirSpeedChangeRate;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
					_fallTimeoutDelta -= NetworkManager.LocalTime.FixedDeltaTime;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < TERMINAL_VELOCITY)
				_verticalVelocity += Gravity * NetworkManager.LocalTime.FixedDeltaTime;
		}
		private void Transform(Vector3 position, Vector2 lookRotation)
		{
			// Update camera target pitch
			CameraSocket.transform.localRotation = Quaternion.Euler(lookRotation.x, 0.0f, 0.0f);

			// rotate the player left and right
			transform.rotation = Quaternion.Euler(0, lookRotation.y, 0);

			// move player
			Vector3 movement = position - transform.position;
			_controller.Move(movement);
		}
		private bool StateBuffered(int Tick, out BufferedState state)
		{
			for (int i = _bufferedStates.Count - 1; i >= 0; i--)
			{
				if (_bufferedStates[i].Tick == Tick)
				{
					state = _bufferedStates[i];
					return true;
				}
			}
			state = null;
			return false;
		}
		private class BufferedState
		{
			public readonly int Tick;
			public readonly NetworkTransformState TransformState;
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