using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Auto Weapon", menuName = "CustomObjects/Weapons/Wheellock")]
    public class Wheellock : ADSWeapon
    {
        [Header("ProjectileInfo")]
        [SerializeField] private ProjectileInfo firstShotInfo;
        [SerializeField] private ProjectileInfo secondShotInfo;
        [Header("Time")]
        [SerializeField] private float Cooldown;
        [SerializeField] private float TimeUntilSecondShot;
        [Header("SprayAmountDegrees")]
        [SerializeField] [MinMaxRange(0, 5)] private RangeSlider firstShotSpray;
        [SerializeField] [MinMaxRange(0, 5)] private RangeSlider firstShotSprayADS;
        [SerializeField] [MinMaxRange(2, 15)] private RangeSlider secondShotSpray;

        [Header("SprayTimers")]
        [SerializeField] private float ShotsUntilMaxSpray;
        [SerializeField] private float SprayResetTime;
        [SerializeField] private int SpraySeed;
        [SerializeField] private float MovementError;
        [Header("Audio")]
        [SerializeField] private AudioClip firstShotAudio;
        [SerializeField] private AudioClip secondShotAudio;

        private float _cooldown;
        private float _relativeSpray;
        private System.Random _rng;
        private float _sprayDecreaseSpeed;
        private float _sprayIncreaseByShot;
        private float _secondShotTimer;

        private AudioSource[] _gunAudioSource;
        private bool _shot;

        private Transform _firstShotSource;
        private Transform _secondShotSource;
        private const string FIRST_SHOT_SOURCE_NAME = "FirstShotSource";
        private const string SECOND_SHOT_SOURCE_NAME = "SecondShotSource";

        public override void Initialize(ulong ownerId, bool isOwner, Transform weaponTransform, Transform cameraTransform)
        {
            base.Initialize(ownerId, isOwner, weaponTransform, cameraTransform);

            _firstShotSource = _weaponObject.transform.Find(FIRST_SHOT_SOURCE_NAME);
            _secondShotSource = _weaponObject.transform.Find(SECOND_SHOT_SOURCE_NAME);

            if (_rng == null)
                _rng = new System.Random(SpraySeed);
            _sprayDecreaseSpeed = 1f / SprayResetTime;
            _sprayIncreaseByShot = 1f / ShotsUntilMaxSpray;
            _gunAudioSource = _weaponObject.GetComponentsInChildren<AudioSource>();
        }
        public override void StopPrimaryAction()
        {
            _shot = false;
        }
        public override void UpdateWeapon(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            base.UpdateWeapon(weaponInputState, playerState);
            // Spray Calculations
            if (_secondShotTimer > 0)
            {
                _secondShotTimer -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
                if (_secondShotTimer <= 0)
                    SecondShot();
            }
            // Cooldown
            else if (_cooldown > 0)
                _cooldown -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            // first shot shooting
            else if (weaponInputState.PrimaryAction && !InADSTransit && !_shot)
                FirstShot();
            else
                _relativeSpray -= NetworkManager.Singleton.LocalTime.FixedDeltaTime * _sprayDecreaseSpeed;

            _relativeSpray = Mathf.Clamp01(_relativeSpray);


            void FirstShot()
            {
                Vector3 randomVector = Quaternion.Euler((float)_rng.NextDouble()*360f-180f, 0, (float)_rng.NextDouble()*360f-180f) * Vector3.up;
                Vector3 shootDirection = GetLookDirection(playerState);
                Vector3 rotationVector = Vector3.Cross(shootDirection, randomVector).normalized;

                float spray = weaponInputState.SecondaryAction ? firstShotSprayADS.Evaluate(_relativeSpray) : firstShotSpray.Evaluate(_relativeSpray);
                float movementError = playerState.Velocity.XZ().magnitude * MovementError;

                Projectile.SpawnProjectile(firstShotInfo, _firstShotSource.position, GetCameraPosition(playerState), Quaternion.AngleAxis(spray + movementError, rotationVector) * shootDirection, _ownerId, weaponInputState.TickDiff);
                _gunAudioSource[0].PlayOneShot(firstShotAudio);

                _secondShotTimer = TimeUntilSecondShot;
                _shot = true;
            }
            void SecondShot()
            {
                Vector3 randomVector = Quaternion.Euler((float)_rng.NextDouble()*360f-180f, 0, (float)_rng.NextDouble()*360f-180f) * Vector3.up;
                Vector3 shootDirection = GetLookDirection(playerState); 
                Vector3 rotationVector = Vector3.Cross(shootDirection, randomVector).normalized;

                float spray = secondShotSpray.Evaluate(_relativeSpray);
                float movementError = playerState.Velocity.XZ().magnitude * MovementError;

                Projectile.SpawnProjectile(secondShotInfo, _secondShotSource.position, GetCameraPosition(playerState), Quaternion.AngleAxis(spray + movementError, rotationVector) * shootDirection, _ownerId, weaponInputState.TickDiff);
                _gunAudioSource[1].PlayOneShot(secondShotAudio);

                _cooldown = Cooldown;
                _relativeSpray += _sprayIncreaseByShot;
            }
        }
    }
}
