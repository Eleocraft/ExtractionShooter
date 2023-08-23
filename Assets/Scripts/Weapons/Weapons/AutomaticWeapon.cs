using UnityEngine;
using Unity.Netcode;

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

        private float _cooldown;
        private float _relativeSpray;
        private bool _shoot;
        private System.Random _rng;

        private float _sprayIncreaseSpeed;
        private float _sprayDecreaseSpeed;
        public override void StartMainAction() => _shoot = true;
        public override void StopMainAction() => _shoot = false;
        private void OnEnable()
        {
            if (_rng == null)
                _rng = new System.Random(SpraySeed);
            _sprayIncreaseSpeed = 1f / MaxSprayReachTime;
            _sprayDecreaseSpeed = 1f / SprayResetTime;
        }
        public override void UpdateWeapon(Vector3 position, Vector3 direction, float velocity, int tick)
        {
            // Spray Calculations
            if (_shoot)
                _relativeSpray += NetworkManager.Singleton.LocalTime.FixedDeltaTime * _sprayIncreaseSpeed;
            else
                _relativeSpray -= NetworkManager.Singleton.LocalTime.FixedDeltaTime * _sprayDecreaseSpeed;

            _relativeSpray = Mathf.Clamp01(_relativeSpray);
            // Cooldown
            if (_cooldown > 0)
                _cooldown -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            // actual shooting
            else if (_shoot)
            {
                Vector3 randomVector = Quaternion.Euler((float)_rng.NextDouble()*720f-360f, 0, (float)_rng.NextDouble()*720f-360f) * Vector3.up;
                Vector3 rotationVector = Vector3.Cross(direction, randomVector).normalized;
                float spray = _relativeSpray * MaxSpray;
                float movementError = MovementError * velocity;
                Projectile.SpawnProjectile(projectileInfo, position, Quaternion.AngleAxis((spray + movementError) * (float)_rng.NextDouble(), rotationVector) * direction, OwnerId, tick);
                _cooldown += Cooldown;
            }
        }
    }
}