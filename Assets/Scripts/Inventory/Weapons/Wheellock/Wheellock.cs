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
        public float MovementError;
        [Header("FirstShot")]
        [SerializeField] private ProjectileInfo FirstShotInfo;
        [SerializeField] private float FirstShotSpray;
        [SerializeField] private float FirstShotSprayADS;
        [SerializeField] private AudioClip FirstShotAudio;
        

        private float _cooldown;

        [HideInInspector] public AudioSource[] GunAudioSource;
        private bool _shot;
        private ItemModifierTag[] _modifierObjects;

        private Transform _firstShotSource;
        [HideInInspector] public Transform SecondShotSource;
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

            _modifierObjects = _weaponObject.transform.GetComponentsInChildren<ItemModifierTag>();

            _firstShotSource = _weaponObject.transform.Find(FIRST_SHOT_SOURCE_NAME);
            SecondShotSource = _weaponObject.transform.Find(SECOND_SHOT_SOURCE_NAME);
            
            GunAudioSource = _weaponObject.GetComponentsInChildren<AudioSource>();
        }
        public override void Deactivate()
        {
            base.Deactivate();

            _cooldown = 0;
            _shot = false;
        }
        public override void UpdateModifier()
        {
            base.UpdateModifier();

            foreach (ItemModifierTag tag in _modifierObjects)
                tag.gameObject.SetActive(false);
            
            _modifierObjects[ActiveModifier].gameObject.SetActive(true);
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
                Projectile.SpawnProjectile(FirstShotInfo, _firstShotSource.position, GetCameraPosition(playerState), direction, OwnerId, weaponInputState.TickDiff);
                GunAudioSource[0].PlayOneShot(FirstShotAudio);
                
                BulletsLoaded--;
                _recoil += Recoil;
            }
            // All glitches
            void SecondShot()
            {
                ((WheellockItemModifier)Modifiers[ActiveModifier]).SecondShot(weaponInputState, playerState, this);

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
        public abstract void SecondShot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState, Wheellock wheellock);
    }
}
