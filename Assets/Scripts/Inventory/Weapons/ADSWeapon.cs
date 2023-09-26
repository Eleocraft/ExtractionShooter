using UnityEngine;
using Cinemachine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class ADSWeapon : Weapon
    {
        [SerializeField] private float ADSFOV;
        [SerializeField] private float TransitTime;
        [SerializeField] private float ADSVelocityMul;
        private CinemachineVirtualCamera _camera;
        private Vector3 _weaponADSPos;
        private Vector3 _weaponDefaultPos;
        private float _defaultFOV;
        private float _adsState;
        protected bool InADSTransit => _adsState > 0 && _adsState < TransitTime;
        private const string ADS_POS_NAME = "WeaponADSPosition";
        public override void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller) {
            base.Initialize(ownerId, isOwner, controller);

            _weaponDefaultPos = transform.localPosition;
            _weaponADSPos = transform.parent.Find(ADS_POS_NAME).localPosition;

            if (isOwner)
            {
                _camera = GameObject.FindGameObjectWithTag(PlayerInterpolation.PLAYER_CAM_TAG).GetComponent<CinemachineVirtualCamera>();
                _defaultFOV = _camera.m_Lens.FieldOfView;
            }
        }
        public override void Deactivate()
        {
            base.Deactivate();

            if (_adsState > 0)
            {
                StopADS();
                _adsState = 0;
            }
                
            if (_camera != null)
                _camera.m_Lens.FieldOfView = _defaultFOV;
        }
        private void StartADS() {
            _firstPersonController.SetMovementSpeedMultiplier(GetInstanceID()+"ADS", ADSVelocityMul);
        }
        private void StopADS() {
            _firstPersonController.SetMovementSpeedMultiplier(GetInstanceID()+"ADS", 1f);
        }
        public override void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            base.UpdateItem(weaponInputState, playerState);

            if (weaponInputState.SecondaryAction && !IsReloading)
            {
                if (_adsState <= 0)
                    StartADS();

                _adsState += NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            }
            else
            {
                if (_adsState > 0 && _adsState - NetworkManager.Singleton.LocalTime.FixedDeltaTime <= 0)
                    StopADS();

                _adsState -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            }
            
            _adsState = Mathf.Clamp(_adsState, 0, TransitTime);

            float relativeTimer = _adsState / TransitTime;

            transform.localPosition = Vector3.Lerp(_weaponDefaultPos, _weaponADSPos, relativeTimer);

            // FOV
            if (_camera != null)
                _camera.m_Lens.FieldOfView = Mathf.Lerp(_defaultFOV, ADSFOV, relativeTimer);
        }
    }
}
