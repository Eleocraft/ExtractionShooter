using UnityEngine;
using Cinemachine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class ADSWeapon : Weapon
    {
        [SerializeField] private float ADSFOV;
        private CinemachineVirtualCamera _camera;
        private float _defaultFOV;
        public override void Initialize(ulong ownerId, bool isOwner, Transform weaponPos)
        {
            base.Initialize(ownerId, isOwner, weaponPos);

            if (isOwner)
            {
                _camera = GameObject.FindGameObjectWithTag(PlayerInterpolation.PLAYER_CAM_TAG).GetComponent<CinemachineVirtualCamera>();
                _defaultFOV = _camera.m_Lens.FieldOfView;
            }
        }
        public override void StartSecondaryAction()
        {
            if (_camera != null)
                _camera.m_Lens.FieldOfView = ADSFOV;
        }
        public override void StopSecondaryAction()
        {
            if (_camera != null)
                _camera.m_Lens.FieldOfView = _defaultFOV;
        }
    }
}
