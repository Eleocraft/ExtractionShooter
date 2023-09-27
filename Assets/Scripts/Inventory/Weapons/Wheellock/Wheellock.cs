using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Wheellock : ADSWeapon
    {
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
        

        [SerializeField] private AudioSource FirstShotSource;
        public AudioSource SecondShotSource;

        public override int MagSize => 2;
        public override float ReloadTime => BulletsLoaded == 1 ? FirstShotReloadTime : SecondShotReloadTime;
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
                Projectile.SpawnProjectile(FirstShotInfo, FirstShotSource.transform.position, GetCameraPosition(playerState), direction, OwnerId, weaponInputState.TickDiff);
                FirstShotSource.PlayOneShot(FirstShotAudio);
                
                BulletsLoaded--;
                _recoil += Recoil;
            }
            // All glitches
            void SecondShot()
            {
                ((WheellockItemModifier)Modifiers[ActiveModifier]).SecondShot(weaponInputState, playerState);

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
        public abstract void SecondShot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState);
    }
}
