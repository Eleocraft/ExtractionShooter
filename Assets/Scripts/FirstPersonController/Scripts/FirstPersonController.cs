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
		[SerializeField] private float SpeedChangeRate = 10.0f;

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
		[SerializeField] private GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		[SerializeField] private float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		[SerializeField] private float BottomClamp = -90.0f;

		// player
		private float _speed;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		[SerializeField]
		private GlobalInputs GI;

		private InputMaster _controls;
		private CharacterController _controller;

		private const float _threshold = 0.01f;
		private const float _positionAsyncDist = 0.1f;

		// Input Network Variables
		private NetworkVariable<Vector2> _move = new NetworkVariable<Vector2>(writePerm: NetworkVariableWritePermission.Owner);
		private NetworkVariable<float> _xRotation = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
		private NetworkVariable<bool> _sprint = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Owner);
		private NetworkVariable<float> _cinemachineTargetPitch = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);

		// Sync Network Variables
		private NetworkVariable<Vector3> _position = new NetworkVariable<Vector3>();

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;

			if (IsOwner)
			{
				GameObject.FindGameObjectWithTag("PlayerCam").GetComponent<Cinemachine.CinemachineVirtualCamera>().Follow = CinemachineCameraTarget.transform;
				_controls = GI.Controls;
				_controls.Player.Jump.started += JumpInput;
			}
		}
		public override void OnNetworkSpawn()
		{
			if (!IsServer)
				_position.OnValueChanged += SyncPosition;
		}
		public override void OnDestroy()
		{
			base.OnDestroy();
			if (!IsServer)
				_position.OnValueChanged -= SyncPosition;

			if (IsOwner)
				_controls.Player.Jump.started -= JumpInput;
		}

		private void JumpInput(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
		{
			Jump();
			if (!IsHost)
				JumpServerRpc();
		}
		[ServerRpc()]
		private void JumpServerRpc() => Jump();
		private void Jump()
		{
			// Jump
			if (Grounded && _jumpTimeoutDelta <= 0.0f)
				// the square root of H * -2 * G = how much velocity needed to reach desired height
				_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity); 
		}
		private void Update()
		{
			if (IsOwner)
				ReadInput();
				
			if (IsServer || IsOwner)
			{
				CalculateGravity();
				GroundedCheck();
				Move();
			}
			if (IsServer)
				_position.Value = transform.position;
		}
		private void ReadInput()
		{
			_move.Value = _controls.Player.Move.ReadValue<Vector2>();
			_sprint.Value = _controls.Player.Sprint.ReadValue<float>().AsBool();

			// Camera rotation
			Vector2 lookInput = _controls.Player.Look.ReadValue<Vector2>();
			// if there is an input
			if (lookInput.sqrMagnitude >= _threshold)
			{
				_cinemachineTargetPitch.Value += lookInput.y * RotationSpeed;
				float _rotationVelocity = lookInput.x * RotationSpeed;

				// clamp our pitch rotation
				_cinemachineTargetPitch.Value = ClampAngle(_cinemachineTargetPitch.Value, BottomClamp, TopClamp);

				_xRotation.Value += _rotationVelocity;
			}
		}

		private void LateUpdate()
		{
			if (!IsOwner && !IsServer) return;

			CameraRotation();
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			// Update Cinemachine camera target pitch
			CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch.Value, 0.0f, 0.0f);

			// rotate the player left and right
			transform.rotation = Quaternion.Euler(0, _xRotation.Value, 0);
		}

		private void Move()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _sprint.Value ? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_move.Value == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Utility.Round(_speed, 0.001f);
			}
			else
				_speed = targetSpeed;

			Vector3 inputDirection = transform.right * _move.Value.x + transform.forward * _move.Value.y;

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
		}

		private void CalculateGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
					_verticalVelocity = -2f;

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
					_jumpTimeoutDelta -= Time.deltaTime;
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
					_fallTimeoutDelta -= Time.deltaTime;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
				_verticalVelocity += Gravity * Time.deltaTime;
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}
		private void SyncPosition(Vector3 oldPosition, Vector3 position)
		{
			if (IsOwner)
			{
				if (position.sqrMagnitude - transform.position.sqrMagnitude > _positionAsyncDist)
					transform.position = position;
				return;	
			}
			// Interpolation here
			transform.position = position;
		}
	}
}