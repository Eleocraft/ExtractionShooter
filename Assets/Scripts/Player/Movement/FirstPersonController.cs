using UnityEngine;
using Unity.Netcode;
using System;

namespace ExoplanetStudios.ExtractionShooter
{
	[RequireComponent(typeof(CharacterController))]
	public class FirstPersonController : PlayerNetworkController
	{
		[Header("Player")]
		[SerializeField] private PlayerInterpolation Playermodel;
		[Tooltip("Slow walk speed of the character in m/s")]
		[SerializeField] private float WalkSpeed = 4.0f;
		[Tooltip("Run speed of the character in m/s")]
		[SerializeField] private float RunSpeed = 6.0f;
		[Tooltip("Crouch speed of the character in m/s")]
		[SerializeField] private float CrouchSpeed = 2.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSensitivity = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		[SerializeField] private float GroundAccelerationRate = 8.0f;
		[SerializeField] private float GroundDecelerationRate = 8.0f;
		[SerializeField] private float AirSpeedChangeRate = 3.0f;
		[Header("Crouch")]
		[SerializeField] private float CrouchEnterSpeed = 10f;

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
		[Tooltip("How far in degrees can you move the camera up")]
		[SerializeField] private float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		[SerializeField] private float BottomClamp = -90.0f;

		[Header("Inputs")]
		[SerializeField] private GlobalInputs GI;
		[Header("Test")]
		[SerializeField] private bool Walk;

		// player
		private float _jumpVelocity;
		private bool _jump; // Owner only
		private Vector2 _lookDelta; // Owner only
		private const float TERMINAL_VELOCITY = 100.0f;

		// timeout timers
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;
		private const float SPEED_OFFSET = 0.05f;
		

		private InputMaster _controls; // Owneronly
		private CharacterController _controller;
		private PlayerEffectManager _effectManager;

		// Interpolation
		[HideInInspector] public PlayerInterpolation PlayerModel;

		// Constants
		private const float MOVEMENT_ERROR_THRESHOLD = 0.02f;
		private const float ROTATION_ERROR_THRESHOLD = 0.02f;

		private void Awake()
		{
			// Instantiate the playermodel
			PlayerModel = Instantiate(Playermodel);
		}
		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();

			// the square root of H * -2 * G = how much velocity needed to reach desired height
			_jumpVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
			_controller = GetComponent<CharacterController>();
			_effectManager = GetComponent<PlayerEffectManager>();
			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
			// Subscribe to tick and OnServerStateChanged events
			NetworkManager.NetworkTickSystem.Tick += Tick;
			// reset Network Variables
			if (IsOwner)
			{
				PlayerModel.SetOwner();
				_controls = GI.Controls;
				_controls.Player.Jump.started += JumpInput;
			}
		}
		public override void OnDestroy()
		{
			base.OnDestroy();

			if (NetworkManager?.NetworkTickSystem != null)
				NetworkManager.NetworkTickSystem.Tick -= Tick;

			
			Destroy(PlayerModel);

			if (IsOwner)
				_controls.Player.Jump.started -= JumpInput;
		}

		private void JumpInput(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => _jump = true;
		public void SetPosition(Vector3 position)
		{
			if (!IsServer) return;

			transform.position = position;
			_currentTransformState = CreateTransformState();
			SetServerTransformStates(_currentTransformState);
			_lastSaveTransform = _currentTransformState;
		}
		private void Tick()
		{
			if (!IsOwner && !IsServer && _currentTransformState.Tick < NetworkManager.LocalTime.Tick - 1) { // Extrapolation if ticks are missed
				Vector3 movement = _currentTransformState.Velocity * NetworkManager.LocalTime.FixedDeltaTime;
				_controller.Move(movement);

                PlayerNetworkTransformState transformState = new(_currentTransformState, NetworkManager.LocalTime.Tick) { Position = transform.position };
                PlayerModel.SetInterpolationStates(transformState);
			}
			
			TransformStateChanged?.Invoke(_currentTransformState);
		}
		protected override void CorrectState(PlayerNetworkTransformState newTransformState) {

			transform.position = newTransformState.Position;
			transform.rotation = Quaternion.Euler(0, newTransformState.LookRotation.y, 0);
			PlayerModel.SetInterpolationStates(newTransformState);
		}
		protected override bool ErrorThresholdPassed(PlayerNetworkTransformState receivedState, PlayerNetworkTransformState currentState) {
			return (currentState.Position - receivedState.Position).sqrMagnitude > MOVEMENT_ERROR_THRESHOLD
						|| (currentState.LookRotation - receivedState.LookRotation).sqrMagnitude > ROTATION_ERROR_THRESHOLD;
		}
		protected override PlayerNetworkInputState CreateInputState()
		{
			return new PlayerNetworkInputState(NetworkManager.LocalTime.Tick,
				Walk ? Vector2.up : _controls.Player.Move.ReadValue<Vector2>(), _lookDelta,
				_controls.Player.Run.IsPressed(), _jump, _controls.Player.Crouch.IsPressed());
		}
		protected override PlayerNetworkTransformState CreateTransformState()
		{
			return new PlayerNetworkTransformState(NetworkManager.LocalTime.Tick,
				transform.position, Vector3.forward, Vector3.zero, 0, 1);
		}
		protected override void ExecuteInput(PlayerNetworkInputState state)
		{
			PlayerNetworkInputState inputState = state as PlayerNetworkInputState;
			if (inputState == null)
				return;

			// lookRotation
			Vector2 lookRotation = GetLookRotation(inputState.LookDelta);

			// start interpolation state
			PlayerModel.SetStartInterpolationState(lookRotation);

			// rotation
			transform.rotation = Quaternion.Euler(0, lookRotation.y, 0);

			// crouch
			float crouchAmount = CalculateCrouch(inputState.Crouch, _currentTransformState.CrouchAmount);
			PlayerModel.SetCrouchAmount(crouchAmount);

			// movement
			Vector3 velocity = CalculateVelocity(inputState, crouchAmount > 0, lookRotation);
			Vector3 movement = velocity * NetworkManager.LocalTime.FixedDeltaTime;
			_controller.Move(movement);
			
			// Set new transform state
			_currentTransformState = new PlayerNetworkTransformState(inputState.Tick, transform.position, lookRotation, velocity, crouchAmount, _effectManager.Slowdown);

			// End interpolation state
			PlayerModel.SetEndInterpolationState(_currentTransformState);
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
				PlayerModel.Rotate(GetLookRotation(_lookDelta));
			}
		}
		private void ReadRotationDelta()
		{
			Vector2 lookInput = _controls.Mouse.Look.ReadValue<Vector2>();

			// Camera rotation
			_lookDelta.x += lookInput.y * RotationSensitivity;
			_lookDelta.y += lookInput.x * RotationSensitivity;
		}
		private Vector2 GetLookRotation(Vector2 lookDelta)
		{
			Vector2 lookRotation = _currentTransformState.LookRotation + lookDelta;
			lookRotation.x = Mathf.Clamp(lookRotation.x, BottomClamp, TopClamp);
			lookRotation.y = lookRotation.y.ClampToAngle();
			return lookRotation;
		}
		private float CalculateCrouch(bool crouchInput, float current)
		{
			return Mathf.MoveTowards(current, crouchInput ? 1f : 0f, CrouchEnterSpeed * NetworkManager.LocalTime.FixedDeltaTime);
		}
		private Vector3 CalculateVelocity(PlayerNetworkInputState inputState, bool crouch, Vector2 lookRotation)
		{
			bool grounded = GroundedCheck();
			float verticalVelocity = CalculateGravity(inputState.Jump, grounded);
			
			// set target speed based on if slowWalk or crouch is pressed + what the velocity multiplier is (items + effects ect.)
			float targetSpeed = crouch ? CrouchSpeed : inputState.Run ? RunSpeed : WalkSpeed;

			// target speed is 0 if no key is pressed
			if (inputState.MovementInput == Vector2.zero) targetSpeed = 0.0f;

			Vector2 targetHorizontalVelocity = inputState.MovementInput.normalized.Rotate(lookRotation.y) * targetSpeed * _currentTransformState.SpeedMultiplier;
			Vector2 horizontalVelocity = _currentTransformState.Velocity.XZ();

			// accelerate or decelerate to target speed
			if ((horizontalVelocity - targetHorizontalVelocity).sqrMagnitude > SPEED_OFFSET)
				horizontalVelocity = Vector2.Lerp(horizontalVelocity, targetHorizontalVelocity, NetworkManager.LocalTime.FixedDeltaTime * CalculateAcceleration(grounded, horizontalVelocity, targetHorizontalVelocity));
			else
				horizontalVelocity = targetHorizontalVelocity;

			return horizontalVelocity.AddHeight(verticalVelocity);


			float CalculateAcceleration(bool grounded, Vector2 horizontalVelocity, Vector2 targetHorizontalVelocity)
			{
				if (!grounded)
					return AirSpeedChangeRate;
				
				return (horizontalVelocity.sqrMagnitude < targetHorizontalVelocity.sqrMagnitude) ? GroundAccelerationRate : GroundDecelerationRate;
			}
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
						verticalVelocity = 0.0f;

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
			if (verticalVelocity < TERMINAL_VELOCITY && !grounded)
				verticalVelocity += Gravity * NetworkManager.LocalTime.FixedDeltaTime;
			
			if (HeadblockCheck() && verticalVelocity > 0)
				verticalVelocity = 0;
			
			return verticalVelocity;
		}
	}
}