using UnityEngine;
using Unity.Netcode;

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
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		[SerializeField] private bool Grounded = true;
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
		private float _xRotation;
		private float _yRotation;
		private const float TERMINAL_VELOCITY = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;
		private const float SPEED_OFFSET = 0.05f;
		// tick system
		private float _lastTickTime;
		private float _tickDeltaTime;

		[SerializeField]
		private GlobalInputs GI;

		private InputMaster _controls;
		private CharacterController _controller;

		// Buffer and CSP
		private const int BUFFER_SIZE = 1024;
		private NetworkInputState[] _inputStates = new NetworkInputState[BUFFER_SIZE];
		private NetworkTransformState[] _transformStates = new NetworkTransformState[BUFFER_SIZE];
		public NetworkVariable<NetworkTransformState> ServerTransformState = new NetworkVariable<NetworkTransformState>();
		public NetworkTransformState _previousTransformState;

		private void Start()
		{
			// the square root of H * -2 * G = how much velocity needed to reach desired height
			_jumpVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
			_controller = GetComponent<CharacterController>();
			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
			NetworkManager.NetworkTickSystem.Tick += Tick;
			ServerTransformState.OnValueChanged += OnServerStateChanged;

			if (IsOwner)
			{
				GameObject.FindGameObjectWithTag("PlayerCam").GetComponent<Cinemachine.CinemachineVirtualCamera>().Follow = CameraSocket.transform;
				_controls = GI.Controls;
				_controls.Player.Jump.started += JumpInput;
			}
		}
		public override void OnDestroy()
		{
			base.OnDestroy();
			
			ServerTransformState.OnValueChanged -= OnServerStateChanged;

			if (NetworkManager.Singleton != null)
				NetworkManager.NetworkTickSystem.Tick -= Tick;

			if (IsOwner)
				_controls.Player.Jump.started -= JumpInput;
		}
		private void OnServerStateChanged(NetworkTransformState previouseState, NetworkTransformState newState)
		{
			_previousTransformState = previouseState;
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
			if (Grounded && _jumpTimeoutDelta <= 0.0f)
				_verticalVelocity = _jumpVelocity; 
		}
		private void Tick()
		{
			_tickDeltaTime = Time.time - _lastTickTime;
			_lastTickTime = Time.time;

			int bufferIndex = NetworkManager.NetworkTickSystem.LocalTime.Tick % BUFFER_SIZE;

			if (IsServer || IsOwner)
			{
				CalculateGravity();
				GroundedCheck();
			}
			if (IsOwner)
			{
				NetworkInputState inputState = ReadInput();

				MoveServerRpc(inputState.MovementInput, inputState.Sprint);
				Move(inputState.MovementInput, inputState.Sprint);

				NetworkTransformState transformState = ReadTransformState();

				_inputStates[bufferIndex] = inputState;
				_transformStates[bufferIndex] = transformState;
			}
			else
			{
				transform.position = ServerTransformState.Value.Position;
				transform.rotation = ServerTransformState.Value.Rotation;
			}
		}
		private void Update()
		{
			if (IsOwner)
			{
				CalculateRotation();
				Rotate(new(_xRotation, _yRotation));
			}
		}
		private NetworkInputState ReadInput()
		{
			return new NetworkInputState()
			{
				Tick = NetworkManager.NetworkTickSystem.LocalTime.Tick,
				MovementInput = _controls.Player.Move.ReadValue<Vector2>(),
				Sprint = _controls.Player.Sprint.ReadValue<float>().AsBool(),
				LookRotation = new Vector2(_xRotation, _yRotation)
			};
		}
		private NetworkTransformState ReadTransformState()
		{
			return new NetworkTransformState()
			{
				Tick = NetworkManager.NetworkTickSystem.LocalTime.Tick,
				Position = transform.position,
				Rotation = transform.rotation
			};
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}
		private void CalculateRotation()
		{
			Vector2 lookInput = _controls.Mouse.Look.ReadValue<Vector2>();

			// Camera rotation
			_xRotation += lookInput.y * RotationSpeed;
			float _rotationVelocity = lookInput.x * RotationSpeed;

			// clamp our pitch rotation
			_xRotation = Mathf.Clamp(_xRotation, BottomClamp, TopClamp);

			_yRotation += _rotationVelocity;
		}

		private void Rotate(Vector2 LookRotation)
		{
			// Update camera target pitch
			CameraSocket.transform.localRotation = Quaternion.Euler(LookRotation.x, 0.0f, 0.0f);

			// rotate the player left and right
			transform.rotation = Quaternion.Euler(0, LookRotation.y, 0);
		}

		[ServerRpc]
		private void MoveServerRpc(Vector2 moveInput, bool sprint)
		{
			Move(moveInput, sprint);
			NetworkTransformState transformState = ReadTransformState();
			// ??!!
			_previousTransformState = ServerTransformState.Value;

			ServerTransformState.Value = transformState;
		}
		private void Move(Vector2 moveInput, bool sprint)
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = sprint ? SprintSpeed : MoveSpeed;

			// target speed is 0 if no key is pressed
			if (moveInput == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - SPEED_OFFSET || currentHorizontalSpeed > targetSpeed + SPEED_OFFSET)
			{
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, _tickDeltaTime * _speedChangeRate);
				_speed = Utility.Round(_speed, 0.001f);
			}
			else
				_speed = targetSpeed;

			Vector3 inputDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

			// move the player
			// -----------> TARGET SPEED FIX
			_controller.Move(inputDirection.normalized * (targetSpeed * _tickDeltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * _tickDeltaTime);
		}

		private void CalculateGravity()
		{
			if (Grounded)
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
					_jumpTimeoutDelta -= _tickDeltaTime;
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// SpeedChangeRate
				_speedChangeRate = AirSpeedChangeRate;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
					_fallTimeoutDelta -= _tickDeltaTime;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < TERMINAL_VELOCITY)
				_verticalVelocity += Gravity * _tickDeltaTime;
		}
	}
}