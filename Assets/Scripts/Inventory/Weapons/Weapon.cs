using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class Weapon : ItemObject
    {
        [SerializeField] private int Seed;
        [SerializeField] private float RecoilDecreaseSpeed = 1;
        [SerializeField] private AudioClip ReloadSound;
        
        private System.Random _rng;
        public abstract int MagSize { get; }
        public abstract float ReloadTime { get; }
        protected int BulletsLoaded {
            get => Ammunition;
            set {
                    
                MagazineDisplay.SetMagazineInfo(OwnerId, value, MagSize);
                Ammunition = value;
            }
        }
        private float _reloadTimer;
        protected bool IsReloading => _reloadTimer > 0;
        protected float _recoil;

        public override void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller) {
            
            base.Initialize(ownerId, isOwner, controller);

            BulletsLoaded = MagSize;
            
            if (_rng == null)
                _rng = new System.Random(Seed);
        }
        public override void Activate() {
            base.Activate();

            MagazineDisplay.SetMagazineInfo(OwnerId, BulletsLoaded, MagSize, true);
        }
        public override void Deactivate() {
            base.Deactivate();
            
            MagazineDisplay.SetMagazineInfo(OwnerId, 0, 0, false);

            _reloadTimer = 0;
            _recoil = 0;
            _firstPersonController.PlayerModel.SetRecoil(0);
            transform.localRotation = Quaternion.identity; // Temp
        }
        public override void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            if (_recoil > 0)
            {
                _recoil -= NetworkManager.Singleton.LocalTime.FixedDeltaTime * RecoilDecreaseSpeed;
                if (_recoil < 0)
                    _recoil = 0;
            }
            _firstPersonController.PlayerModel.SetRecoil(_recoil);

            if (_reloadTimer > 0)
            {
                _reloadTimer -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
                transform.localRotation = Quaternion.AngleAxis(Mathf.Clamp01(_reloadTimer / ReloadTime) * 360, Vector3.forward); // Temp
                if (_reloadTimer <= 0)
                    BulletsLoaded = MagSize;
            }
        }

        public override void Reload() {
            if (BulletsLoaded == MagSize || IsReloading)
                return;
            
            _reloadTimer = ReloadTime;
            SFXSource.Source.PlayOneShot(ReloadSound);
        }

        public Vector3 GetShootDirection(NetworkTransformState playerState, float spray, float maxMovementError)
        {
            Vector3 randomVector = Quaternion.Euler((float)_rng.NextDouble()*360f-180f, 0, (float)_rng.NextDouble()*360f-180f) * Vector3.up;
            Vector3 shootDirection = GetLookDirection(playerState);
            shootDirection = Quaternion.AngleAxis(-_recoil, Quaternion.Euler(0, 90, 0) * shootDirection.WithHeight(0).normalized) * shootDirection;
            Vector3 rotationVector = Vector3.Cross(shootDirection, randomVector).normalized;

            return Quaternion.AngleAxis((spray + maxMovementError * playerState.Velocity.magnitude) * (float)_rng.NextDouble(), rotationVector) * shootDirection;
        }
    }
}