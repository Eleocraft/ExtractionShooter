using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    [CreateAssetMenu(fileName = "New Wheellock Weapon", menuName = "CustomObjects/Weapons/Wheellock")]
    public class Wheellock : ADSWeapon
    {
        [Header("ProjectileInfo")]
        [SerializeField] private ProjectileInfo FirstShotInfo;
        [SerializeField] private ProjectileInfo SecondShotInfo;
        [Header("Time")]
        [SerializeField] private float Cooldown;
        [Header("SprayAmountDegrees")]
        [SerializeField] private float FirstShotSpray;
        [SerializeField] private float FirstShotSprayADS;
        [SerializeField] private float SecondShotSpray;
        [SerializeField] private float MovementError;
        [Header("Reload")]
        [SerializeField] private float FirstShotReloadTime;
        [SerializeField] private float SecondShotReloadTime;
        [Header("Audio")]
        [SerializeField] private AudioClip FirstShotAudio;
        [SerializeField] private AudioClip SecondShotAudio;

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
            }
            void SecondShot()
            {
                Vector3 direction = GetShootDirection(playerState, SecondShotSpray, MovementError);
                Projectile.SpawnProjectile(SecondShotInfo, _secondShotSource.position, GetCameraPosition(playerState), direction, _ownerId, weaponInputState.TickDiff);
                _gunAudioSource[1].PlayOneShot(SecondShotAudio);

                BulletsLoaded--;
            }
        }
    }
}
