using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class Weapon : ItemObject
    {
        [SerializeField] private GameObject WeaponPrefab;
        
        [SerializeField] private int Seed;
        
        private System.Random _rng;

        protected Transform _weaponParent;

        protected GameObject _weaponObject;
        public abstract int MagSize { get; }
        public abstract float ReloadTime { get; }
        private int _bulletsLoaded;
        protected int BulletsLoaded {
            get => _bulletsLoaded;
            set {
                if (_isOwner)
                    MagazineDisplay.SetMagazineInfo(value, MagSize);
                _bulletsLoaded = value;
            }
        }
        private float _reloadTimer;
        protected bool IsReloading => _reloadTimer > 0;

        public override void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller) {
            
            base.Initialize(ownerId, isOwner, controller);

            _weaponParent = controller.PlayerModel.WeaponTransform;
            BulletsLoaded = MagSize;
            
            if (_rng == null)
                _rng = new System.Random(Seed);
        }
        public override void Activate() {
            base.Activate();

            _weaponObject = Instantiate(WeaponPrefab, _weaponParent);
            if (_isOwner)
            {
                MagazineDisplay.Activate();
                MagazineDisplay.SetMagazineInfo(BulletsLoaded, MagSize);
                _weaponObject.SetLayerAllChildren(LayerMask.NameToLayer("First Person"));
            }
        }
        public override void Deactivate() {
            base.Deactivate();
            
            if (_isOwner)
                MagazineDisplay.Deactivate();

            _reloadTimer = 0;
            Destroy(_weaponObject);
        }
        public override void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            if (_reloadTimer > 0)
            {
                _reloadTimer -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
                _weaponObject.transform.localRotation = Quaternion.AngleAxis(Mathf.Clamp01(_reloadTimer / ReloadTime) * 360, Vector3.right); // Temp
                if (_reloadTimer <= 0)
                    BulletsLoaded = MagSize;
            }
        }

        public override void Reload() {
            if (BulletsLoaded == MagSize || IsReloading)
                return;
            
            _reloadTimer = ReloadTime;
        }

        protected Vector3 GetShootDirection(NetworkTransformState playerState, float spray, float maxMovementError)
        {
            Vector3 randomVector = Quaternion.Euler((float)_rng.NextDouble()*360f-180f, 0, (float)_rng.NextDouble()*360f-180f) * Vector3.up;
            Vector3 shootDirection = GetLookDirection(playerState); 
            Vector3 rotationVector = Vector3.Cross(shootDirection, randomVector).normalized;

            return Quaternion.AngleAxis((spray + maxMovementError * playerState.Velocity.XZ().magnitude) * (float)_rng.NextDouble(), rotationVector) * shootDirection;
        }
    }
}