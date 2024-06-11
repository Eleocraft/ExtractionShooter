using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Rifle : ADSWeapon
    {
        protected override float ADSFOV => ((RifleItemModifier)Modifiers[ActiveModifier]).ADSFov;
        protected override bool CanADS => ((RifleItemModifier)Modifiers[ActiveModifier]).CanADS;
        public Transform ShotSource;
        [SerializeField] private GameObject ScopePP;
        [Header("Recoil")]
        [SerializeField] private float Recoil;
        public float MovementError;
        [Header("Reload")]
        [SerializeField] private float ShotReloadTime;
        public override int MagSize => ((RifleItemModifier)Modifiers[ActiveModifier]).MagSize;

        public override float ReloadTime => ShotReloadTime;
        protected override Vector3 ADSPos => ((RifleItemModifier)Modifiers[ActiveModifier]).ADSPos;

        private bool _shot;
        public override void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller)
        {
            base.Initialize(ownerId, isOwner, controller);

            if (!isOwner)
                Destroy(ScopePP.gameObject);
        }
        public override void Deactivate()
        {
            base.Deactivate();

            _shot = false;
        }

        public override void UpdateItem(NetworkWeaponInputState weaponInputState, PlayerNetworkTransformState playerState)
        {
            base.UpdateItem(weaponInputState, playerState);

            if (_shot && !weaponInputState.PrimaryAction)
                _shot = false;

            // first shot shooting
            if (weaponInputState.PrimaryAction && !InADSTransit && !_shot && !IsReloading)
            {
                if (BulletsLoaded > 0 && ((RifleItemModifier)Modifiers[ActiveModifier]).Shot(weaponInputState, playerState, _isOwner))
                {
                    BulletsLoaded--;
                    _recoil += Recoil;
                }
                _shot = true;
            }
            ((RifleItemModifier)Modifiers[ActiveModifier]).UpdateItem(InADS || InADSTransit);
        }
    }
    public abstract class RifleItemModifier : ItemModifier
    {
        [SerializeField] protected AudioClip Audio;
        [SerializeField] protected ProjectileInfo Info;
        [SerializeField] protected float Spray;
        [SerializeField] protected float SprayADS;
        public float ADSFov;
        public abstract int MagSize { get; }
        public virtual bool CanADS => true;
        public virtual Vector3 ADSPos => new Vector3(0, -0.05f, 0.3f);
        
        public abstract bool Shot(NetworkWeaponInputState weaponInputState, PlayerNetworkTransformState playerState, bool isOwner);
        public virtual void UpdateItem(bool ADS) { }
    }
}
