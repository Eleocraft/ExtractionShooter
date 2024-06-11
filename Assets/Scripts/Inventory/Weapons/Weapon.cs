using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class Weapon : ItemObject
    {
        [SerializeField] private int Seed;
        [SerializeField] private float RecoilDecreaseSpeed = 1;
        [SerializeField] private AudioClip ReloadSound;
        public AudioSource audioSource;
        
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
        public override void UpdateItem(NetworkWeaponInputState weaponInputState, PlayerNetworkTransformState playerState)
        {
            if (_recoil > 0)
            {
                _recoil -= NetworkManager.Singleton.LocalTime.FixedDeltaTime * RecoilDecreaseSpeed;
                if (_recoil < 0)
                    _recoil = 0;
            }
            _firstPersonController.PlayerModel.SetRecoil(_recoil);

            if (weaponInputState.ReloadAction)
                Reload();
                
            if (_reloadTimer > 0)
            {
                _reloadTimer -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
                transform.localRotation = Quaternion.AngleAxis(Mathf.Clamp01(_reloadTimer / ReloadTime) * 360, Vector3.forward); // Temp
                if (_reloadTimer <= 0)
                    FinishReload();
            }
        }
        public override void UpdateModifier()
        {
            base.UpdateModifier();

            FinishReload();
        }

        private void Reload() {
            if (BulletsLoaded == MagSize || IsReloading)
                return;
            
            _reloadTimer = ReloadTime;
            if (_isOwner)
                SFXSource.PlaySoundEffect(ReloadSound);
            else
                audioSource.PlayOneShot(ReloadSound);
        }
        private void FinishReload() {
            BulletsLoaded = MagSize;
        }
        int RANDOM_VEC_X_SEED = 1023840;
        int RANDOM_VEC_Y_SEED = 238972;
        int RANDOM_SPRAY_AMOUNT_SEED = 271254;

        public Vector3 GetShootDirection(PlayerNetworkTransformState playerState, float spray, float maxMovementError)
        {
            Vector3 randomVector = Quaternion.Euler(NetworkRNG.Value(playerState.Tick, RANDOM_VEC_X_SEED)*360f-180f, 0, NetworkRNG.Value(playerState.Tick, RANDOM_VEC_Y_SEED)*360f-180f) * Vector3.up;
            Vector3 shootDirection = GetLookDirection(playerState);
            shootDirection = Quaternion.AngleAxis(-_recoil, Quaternion.Euler(0, 90, 0) * shootDirection.WithHeight(0).normalized) * shootDirection;
            Vector3 rotationVector = Vector3.Cross(shootDirection, randomVector).normalized;
            
            return Quaternion.AngleAxis((spray + maxMovementError * playerState.Velocity.magnitude) * NetworkRNG.Value(playerState.Tick, RANDOM_SPRAY_AMOUNT_SEED), rotationVector) * shootDirection;
        }
    }
}