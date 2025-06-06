using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Wheellock : ADSWeapon
    {
        [SerializeField] private float AdsFov;
        [SerializeField] private Vector3 AdsPos;
        protected override Vector3 ADSPos => AdsPos;
        protected override float ADSFOV => AdsFov;
        [Header("Time")]
        [SerializeField] private float Cooldown;
        [Header("Reload")]
        [SerializeField] private float FirstShotReloadTime;
        [SerializeField] private float SecondShotReloadTime;
        [Header("Recoil")]
        [SerializeField] private float Recoil;
        public float MovementError;
        [Header("FirstShot")]
        [SerializeField] private ProjectileInfo FirstShotInfo;
        [SerializeField] private float FirstShotSpray;
        [SerializeField] private float FirstShotSprayADS;
        [SerializeField] private AudioClip FirstShotAudio;
        

        private float _cooldown;
        private bool _shot;
        

        [SerializeField] private Transform FirstShotSource;
        public Transform SecondShotSource;

        public override int MagSize => 2;
        public override float ReloadTime => BulletsLoaded == 1 ? FirstShotReloadTime : SecondShotReloadTime;
        public override void Deactivate()
        {
            base.Deactivate();

            _cooldown = 0;
            _shot = false;
        }
        public override void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            base.UpdateItem(weaponInputState, playerState);

            if (_shot && !weaponInputState.PrimaryAction)
                _shot = false;
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
                Projectile.SpawnProjectile(FirstShotInfo, FirstShotSource.position, GetCameraPosition(playerState), direction, OwnerId, weaponInputState.TickDiff);
                if (_isOwner)
                    SFXSource.PlaySoundEffect(FirstShotAudio);
                else
                    audioSource.PlayOneShot(FirstShotAudio);
                
                BulletsLoaded--;
                _recoil += Recoil;
            }
            // All glitches
            void SecondShot()
            {
                ((WheellockItemModifier)Modifiers[ActiveModifier]).SecondShot(weaponInputState, playerState, _isOwner);

                BulletsLoaded--;
                _recoil += Recoil;
            }
        }
    }
    public abstract class WheellockItemModifier : ItemModifier
    {
        [SerializeField] protected AudioClip Audio;
        [SerializeField] protected ProjectileInfo Info;
        [SerializeField] protected float Spray;
        [SerializeField] protected float SprayADS;
        public abstract void SecondShot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState, bool isOwner);
    }
}
