using UnityEngine;
using Cinemachine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class ADSWeapon : Weapon
    {
        [SerializeField] private float ADSFOV;
        [SerializeField] private float TransitTime;
        [SerializeField] private float VelocityMultiplier;
        private CinemachineVirtualCamera _camera;
        private FirstPersonController _controller;
        private Vector3 _weaponADSPos;
        private Vector3 _weaponDefaultPos;
        private float _defaultFOV;
        private float _timer;
        protected bool InADSTransit => _timer > 0 && _timer < TransitTime;
        private const string ADS_POS_NAME = "WeaponADSPosition";
        public override void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller) {
            base.Initialize(ownerId, isOwner, controller);

            _controller = controller;

            if (isOwner)
            {
                _camera = GameObject.FindGameObjectWithTag(PlayerInterpolation.PLAYER_CAM_TAG).GetComponent<CinemachineVirtualCamera>();
                _defaultFOV = _camera.m_Lens.FieldOfView;
            }
        }
        public override void Activate()
        {
            base.Activate();

            _weaponDefaultPos = _weaponObject.transform.localPosition;
            _weaponADSPos = _weaponObject.transform.parent.Find(ADS_POS_NAME).localPosition;
        }
        public override void Deactivate()
        {
            base.Deactivate();

            if (_timer > 0)
                StopADS();
                
            if (_camera != null)
                _camera.m_Lens.FieldOfView = _defaultFOV;
        }
        private void StartADS() {
            _controller.IncreaseMovementVelocityMultiplier(VelocityMultiplier);
        }
        private void StopADS() {
            _controller.DecreaseMovementVelocityMultiplier(VelocityMultiplier);
        }
        public override void UpdateWeapon(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            base.UpdateWeapon(weaponInputState, playerState);

            if (weaponInputState.SecondaryAction && !IsReloading)
            {
                if (_timer <= 0)
                    StartADS();

                _timer += NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            }
            else
            {
                if (_timer > 0 && _timer - NetworkManager.Singleton.LocalTime.FixedDeltaTime <= 0)
                    StopADS();

                _timer -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            }
            
            _timer = Mathf.Clamp(_timer, 0, TransitTime);

            float relativeTimer = _timer / TransitTime;

            _weaponObject.transform.localPosition = Vector3.Lerp(_weaponDefaultPos, _weaponADSPos, relativeTimer);

            // FOV
            if (_camera != null)
                _camera.m_Lens.FieldOfView = Mathf.Lerp(_defaultFOV, ADSFOV, relativeTimer);
        }
    }
}
