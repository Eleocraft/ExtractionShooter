using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerInterpolation : MonoBehaviour
    {
        [SerializeField] private Transform CameraSocket;

        private InterpolationState _lerpStartInterpolationState;
		private InterpolationState _lerpEndInterpolationState;
        private float _currentTickDeltaTime;        
        private bool _isOwner;

        private const string CAMERA_POS_NAME = "CameraPos";
		private const string PLAYER_CAM_TAG = "PlayerCam";
        public void SetOwner()
        {
            _isOwner = true;
            GameObject.FindGameObjectWithTag(PLAYER_CAM_TAG).GetComponent<Cinemachine.CinemachineVirtualCamera>().Follow = CameraSocket.Find(CAMERA_POS_NAME);
            
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>()) // Set owner invisible
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
        public void SetInterpolationStates(Vector2 lookRotation, NetworkTransformState newState)
        {
            SetStartInterpolationState(lookRotation);
            SetEndInterpolationState(newState);
        }
        public void SetStartInterpolationState(Vector2 lookRotation)
        {
            _lerpStartInterpolationState = new (transform.position, lookRotation);
        }
        public void SetEndInterpolationState(NetworkTransformState currentTransformState)
        {
            _lerpEndInterpolationState = new (currentTransformState);
            _currentTickDeltaTime = 0;
        }
        private void Start()
        {
            _lerpStartInterpolationState = new InterpolationState(transform.position, Vector2.zero);
			_lerpEndInterpolationState = new InterpolationState(transform.position, Vector2.zero);
        }
        private void Update()
		{
			_currentTickDeltaTime += Time.deltaTime;
			float relativeDeltaTime = _currentTickDeltaTime / NetworkManager.Singleton.LocalTime.FixedDeltaTime;

			transform.position = Vector3.Lerp(_lerpStartInterpolationState.Position, _lerpEndInterpolationState.Position, relativeDeltaTime);

			// the owner should always have the most accurate lookrotation possible
			if (!_isOwner)
			    Rotate(Utility.Vector2RotateLerp(_lerpStartInterpolationState.LookRotation, _lerpEndInterpolationState.LookRotation, relativeDeltaTime));
		}
        public void Rotate(Vector2 lookRotation)
		{
			// Update camera target pitch
			CameraSocket.localRotation = Quaternion.Euler(lookRotation.x, 0.0f, 0.0f);

			// rotate the player left and right
			transform.rotation = Quaternion.Euler(0, lookRotation.y, 0);
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