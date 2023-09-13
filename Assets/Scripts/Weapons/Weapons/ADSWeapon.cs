using UnityEngine;
using Cinemachine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class ADSWeapon : Weapon
    {
        [SerializeField] private float ADSFOV;
        [SerializeField] private float TransitTime;
        private CinemachineVirtualCamera _camera;
        private Vector3 _weaponADSPos;
        private Vector3 _weaponDefaultPos;
        private float _defaultFOV;
        private float _timer;
        protected bool InADSTransit => _timer > 0 && _timer < TransitTime;
        private const string ADS_POS_NAME = "WeaponADSPosition";
        public override void Initialize(ulong ownerId, bool isOwner, Transform weaponTransform, Transform cameraTransform)
        {
            base.Initialize(ownerId, isOwner, weaponTransform, cameraTransform);

            _weaponDefaultPos = _weaponObject.transform.localPosition;
            _weaponADSPos = _weaponObject.transform.parent.Find(ADS_POS_NAME).localPosition;

            if (isOwner)
            {
                _camera = GameObject.FindGameObjectWithTag(PlayerInterpolation.PLAYER_CAM_TAG).GetComponent<CinemachineVirtualCamera>();
                _defaultFOV = _camera.m_Lens.FieldOfView;
            }
        }
        public override void UpdateWeapon(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            if (weaponInputState.SecondaryAction)
                _timer += NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            else
                _timer -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            
            _timer = Mathf.Clamp(_timer, 0, TransitTime);

            float relativeTimer = _timer / TransitTime;

            _weaponObject.transform.localPosition = Vector3.Lerp(_weaponDefaultPos, _weaponADSPos, relativeTimer);

            // FOV
            if (_camera != null)
                _camera.m_Lens.FieldOfView = Mathf.Lerp(_defaultFOV, ADSFOV, relativeTimer);
        }
    }
}
