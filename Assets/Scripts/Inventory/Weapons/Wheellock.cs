using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Wheellock Weapon", menuName = "CustomObjects/Weapons/Wheellock")]
    public class Wheellock : ADSWeapon
    {
        [Header("Time")]
        [SerializeField] private float Cooldown;
        [Header("Reload")]
        [SerializeField] private float FirstShotReloadTime;
        [SerializeField] private float SecondShotReloadTime;
        [Header("Recoil")]
        [SerializeField] private float Recoil;
        [Header("FirstShot")]
        [SerializeField] private ProjectileInfo FirstShotInfo;
        [SerializeField] private float FirstShotSpray;
        [SerializeField] private float FirstShotSprayADS;
        [SerializeField] private float MovementError;
        [SerializeField] private AudioClip FirstShotAudio;
        [Header("SecondShot")]
        [SerializeField] private WheellockSecondShotData DefaultSecondShotData;
        [SerializeField] private WheellockSecondShotData StunSecondShotData;
        [SerializeField] private WheellockSecondShotData AccurateSecondShotData;
        [SerializeField] private WheellockSecondShotData ShotgunSecondShotData;
        [SerializeField] private int ShotgunProjectileAmount;

        private float _cooldown;

        private AudioSource[] _gunAudioSource;
        private bool _shot;

        private Transform _firstShotSource;
        private Transform _secondShotSource;
        private const string FIRST_SHOT_SOURCE_NAME = "FirstShotSource";
        private const string SECOND_SHOT_SOURCE_NAME = "SecondShotSource";

        public override int MagSize => 2;
        public override float ReloadTime => BulletsLoaded == 1 ? FirstShotReloadTime : SecondShotReloadTime;

        public override void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller)
        {
            base.Initialize(ownerId, isOwner, controller);
        }
        public override void Activate()
        {
            base.Activate();

            _firstShotSource = _weaponObject.transform.Find(FIRST_SHOT_SOURCE_NAME);
            _secondShotSource = _weaponObject.transform.Find(SECOND_SHOT_SOURCE_NAME);
            
            _gunAudioSource = _weaponObject.GetComponentsInChildren<AudioSource>();
        }
        public override void Deactivate()
        {
            base.Deactivate();

            _cooldown = 0;
            _shot = false;
        }
        public override void StopPrimaryAction()
        {
            _shot = false;
        }
        public override void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            base.UpdateItem(weaponInputState, playerState);

            // Cooldown
            if (_cooldown > 0)
                _cooldown -= NetworkManager.Singleton.LocalTime.FixedDeltaTime;
            // first shot shooting
            else if (weaponInputState.PrimaryAction && !InADSTransit && !_shot && !IsReloading)
            {
                if (BulletsLoaded == 2)
                    FirstShot();
                else if (BulletsLoaded == 1)
                    SecondShot();
                else
                {
                    _shot = true;
                    return;
                }
                
                _shot = true;
                _cooldown = Cooldown;
            }


            void FirstShot()
            {
                float spray = weaponInputState.SecondaryAction ? FirstShotSprayADS : FirstShotSpray;

                Vector3 direction = GetShootDirection(playerState, spray, MovementError);
                Projectile.SpawnProjectile(FirstShotInfo, _firstShotSource.position, GetCameraPosition(playerState), direction, _ownerId, weaponInputState.TickDiff);
                _gunAudioSource[0].PlayOneShot(FirstShotAudio);
                
                BulletsLoaded--;
                _recoil += Recoil;
            }
            // All glitches
            void SecondShot()
            {
                switch (ActiveModifier)
                {
                    case 0:
                        DefaultSecondShot();
                        break;
                    case 1:
                        StunSecondShot();
                        break;
                    case 2:
                        AccurateSecondShot();
                        break;
                    case 3:
                        ShotgunSecondShot();
                        break;
                }

                BulletsLoaded--;
                _recoil += Recoil;
            }
            void DefaultSecondShot()
            {
                float spray = weaponInputState.SecondaryAction ? DefaultSecondShotData.SprayADS : DefaultSecondShotData.Spray;

                Vector3 direction = GetShootDirection(playerState, spray, MovementError);
                Projectile.SpawnProjectile(DefaultSecondShotData.Info, _secondShotSource.position, GetCameraPosition(playerState), direction, _ownerId, weaponInputState.TickDiff);
                _gunAudioSource[1].PlayOneShot(DefaultSecondShotData.Audio);
            }
            void StunSecondShot()
            {
                float spray = weaponInputState.SecondaryAction ? StunSecondShotData.SprayADS : StunSecondShotData.Spray;

                Vector3 direction = GetShootDirection(playerState, spray, MovementError);
                Projectile.SpawnProjectile(StunSecondShotData.Info, _secondShotSource.position, GetCameraPosition(playerState), direction, _ownerId, weaponInputState.TickDiff);
                _gunAudioSource[1].PlayOneShot(StunSecondShotData.Audio);
            }
            void AccurateSecondShot()
            {
                float spray = weaponInputState.SecondaryAction ? AccurateSecondShotData.SprayADS : AccurateSecondShotData.Spray;

                Vector3 direction = GetShootDirection(playerState, spray, MovementError);
                Projectile.SpawnProjectile(AccurateSecondShotData.Info, _secondShotSource.position, GetCameraPosition(playerState), direction, _ownerId, weaponInputState.TickDiff);
                _gunAudioSource[1].PlayOneShot(AccurateSecondShotData.Audio);
            }
            void ShotgunSecondShot()
            {
                float spray = weaponInputState.SecondaryAction ? ShotgunSecondShotData.SprayADS : ShotgunSecondShotData.Spray;

                for (int i = 0; i < ShotgunProjectileAmount; i++)
                {
                    Vector3 direction = GetShootDirection(playerState, spray, MovementError);
                    Projectile.SpawnProjectile(ShotgunSecondShotData.Info, _secondShotSource.position, GetCameraPosition(playerState), direction, _ownerId, weaponInputState.TickDiff);
                }
                _gunAudioSource[1].PlayOneShot(ShotgunSecondShotData.Audio);
            }
        }
        [System.Serializable]
        public class WheellockSecondShotData
        {
            public AudioClip Audio;
            public ProjectileInfo Info;
            public float Spray;
            public float SprayADS;
        }
    }
}
