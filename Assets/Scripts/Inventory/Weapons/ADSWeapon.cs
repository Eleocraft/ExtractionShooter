using UnityEngine;
using Cinemachine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class ADSWeapon : Weapon
    {
        [SerializeField] private float TransitTime;
        [SerializeField] private float ADSVelocityMul;
        private CinemachineVirtualCamera _camera;
        private Vector3 _weaponDefaultPos;
        private float _defaultFOV;
        private float _adsState;
        protected bool InADSTransit => _adsState > 0 && _adsState < TransitTime;
        protected bool InADS => _adsState >= TransitTime;
        protected abstract float ADSFOV { get; }
        protected virtual bool CanADS => true;
        protected abstract Vector3 ADSPos { get; }
        public override void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller) {
            base.Initialize(ownerId, isOwner, controller);

            _weaponDefaultPos = transform.localPosition;

            if (isOwner)
            {
                _camera = GameObject.FindGameObjectWithTag(PlayerInterpolation.PLAYER_CAM_TAG).GetComponent<CinemachineVirtualCamera>();
                _defaultFOV = _camera.m_Lens.FieldOfView;
            }
        }
        public override void Deactivate()
        {
            base.Deactivate();

            if (_adsState > 0) {
                _adsState = 0;
                transform.localPosition = _weaponDefaultPos;
            }
                
            if (_camera != null)
                _camera.m_Lens.FieldOfView = _defaultFOV;
        }
        public override void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            base.UpdateItem(weaponInputState, playerState);
            
            if (CanADS && weaponInputState.SecondaryAction && !IsReloading)
                _adsState += NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            else
                _adsState -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            
            _adsState = Mathf.Clamp(_adsState, 0, TransitTime);

            float relativeTimer = _adsState / TransitTime;

            if (relativeTimer > 0)
                transform.localPosition = Vector3.Lerp(_weaponDefaultPos, transform.parent.localPosition + ADSPos, relativeTimer);

            // FOV
            if (_camera != null)
                _camera.m_Lens.FieldOfView = Mathf.Lerp(_defaultFOV, ADSFOV, relativeTimer);
        }
    }
}
