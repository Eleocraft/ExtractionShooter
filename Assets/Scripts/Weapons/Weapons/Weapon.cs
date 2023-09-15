using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class Weapon : ScriptableObject
    {
        protected ulong _ownerId;
        [SerializeField] private GameObject WeaponPrefab;
        
        [SerializeField] private int Seed;
        
        private System.Random _rng;

        protected Transform _cameraTransform;
        protected Transform _weaponParent;

        protected GameObject _weaponObject;
        public abstract int MagSize { get; }
        public abstract float ReloadTime { get; }
        private int _bulletsLoaded;
        protected int BulletsLoaded {
            get => _bulletsLoaded;
            set {
                MagazineDisplay.SetMagazineInfo(value, MagSize);
                _bulletsLoaded = value;
            }
        }
        private float _reloadTimer;
        protected bool IsReloading => _reloadTimer > 0;

        public virtual void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller) {

            _cameraTransform = controller.PlayerModel.CameraSocket;
            _ownerId = ownerId;
            _weaponParent = controller.PlayerModel.WeaponTransform;
            BulletsLoaded = MagSize;
            
            if (_rng == null)
                _rng = new System.Random(Seed);
        }
        public virtual void Activate() {
            _weaponObject = Instantiate(WeaponPrefab, _weaponParent);
        }
        public virtual void Deactivate() {
            _reloadTimer = 0;
            Destroy(_weaponObject);
        }
        public virtual void UpdateWeapon(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            if (_reloadTimer > 0)
            {
                _reloadTimer -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
                _weaponObject.transform.localRotation = Quaternion.AngleAxis(_reloadTimer / ReloadTime * 360, Vector3.right); // Temp
                if (_reloadTimer <= 0)
                    BulletsLoaded = MagSize;
            }
        }
        public virtual void StartPrimaryAction() { }
        public virtual void StopPrimaryAction() { }
        public virtual void StartSecondaryAction() { }
        public virtual void StopSecondaryAction() { }

        public virtual void Reload() {
            if (BulletsLoaded == MagSize || IsReloading)
                return;
            
            _reloadTimer = ReloadTime;
        }
        
        protected Vector3 GetCameraPosition(NetworkTransformState playerState) => Vector3.up * _cameraTransform.localPosition.y + playerState.Position;
        protected Vector3 GetLookDirection(NetworkTransformState playerState) => Quaternion.Euler(playerState.LookRotation.x, playerState.LookRotation.y, 0) * Vector3.forward;

        protected Vector3 GetShootDirection(NetworkTransformState playerState, float spray, float maxMovementError)
        {
            Vector3 randomVector = Quaternion.Euler((float)_rng.NextDouble()*360f-180f, 0, (float)_rng.NextDouble()*360f-180f) * Vector3.up;
            Vector3 shootDirection = GetLookDirection(playerState); 
            Vector3 rotationVector = Vector3.Cross(shootDirection, randomVector).normalized;

            return Quaternion.AngleAxis((spray + maxMovementError * playerState.Velocity.XZ().magnitude) * (float)_rng.NextDouble(), rotationVector) * shootDirection;
        }
    }
}