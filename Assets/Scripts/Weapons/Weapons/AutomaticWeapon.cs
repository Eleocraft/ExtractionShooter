using UnityEngine;
using Unity.Netcode;
using Cinemachine;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Auto Weapon", menuName = "CustomObjects/Weapons/Auto")]
    public class AutomaticWeapon : Weapon
    {
        [SerializeField] private ProjectileInfo projectileInfo;
        [SerializeField] private float Cooldown;
        [SerializeField] private float MaxSpray;
        [SerializeField] private float MaxSprayReachTime;
        [SerializeField] private float SprayResetTime;
        [SerializeField] private int SpraySeed;
        [SerializeField] private float MovementError;
        [SerializeField] private float ADSFOV;

        private float _cooldown;
        private float _relativeSpray;
        private System.Random _rng;

        private float _sprayIncreaseSpeed;
        private float _sprayDecreaseSpeed;

        private CinemachineVirtualCamera _camera;
        private float _defaultFOV;

        public override void Initialize(bool IsOwner)
        {
            if (_rng == null)
                _rng = new System.Random(SpraySeed);
            _sprayIncreaseSpeed = 1f / MaxSprayReachTime;
            _sprayDecreaseSpeed = 1f / SprayResetTime;

            if (IsOwner)
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
        public override void UpdateWeapon(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState, Vector3 weaponPos, float velocity)
        {
            // Spray Calculations
            if (weaponInputState.PrimaryAction)
                _relativeSpray += NetworkManager.Singleton.LocalTime.FixedDeltaTime * _sprayIncreaseSpeed;
            else
                _relativeSpray -= NetworkManager.Singleton.LocalTime.FixedDeltaTime * _sprayDecreaseSpeed;

            _relativeSpray = Mathf.Clamp01(_relativeSpray);
            // Cooldown
            if (_cooldown > 0)
                _cooldown -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            // actual shooting
            else if (weaponInputState.PrimaryAction)
            {
                Vector3 randomVector = Quaternion.Euler((float)_rng.NextDouble()*360f-180f, 0, (float)_rng.NextDouble()*360f-180f) * Vector3.up;
                Vector3 shootDirection = weaponInputState.SecondaryAction ? GetLookDirection(playerState) : GetShootDirection(weaponPos, playerState);
                Vector3 rotationVector = Vector3.Cross(shootDirection, randomVector).normalized;
                float spray = _relativeSpray * MaxSpray;
                float movementError = velocity * MovementError;
                Projectile.SpawnProjectile(projectileInfo, weaponInputState.SecondaryAction ? GetCameraPosition(playerState) : weaponPos, Quaternion.AngleAxis((spray + movementError) * (float)_rng.NextDouble(), rotationVector) * shootDirection, OwnerId, weaponInputState.TickDiff);
                _cooldown += Cooldown;
            }
        }
    }
}