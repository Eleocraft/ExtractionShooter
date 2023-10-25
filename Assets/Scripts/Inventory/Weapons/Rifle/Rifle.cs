using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Rifle : ADSWeapon
    {
        protected override float ADSFOV => ((RifleItemModifier)Modifiers[ActiveModifier]).ADSFov;
        protected override bool CanADS => ((RifleItemModifier)Modifiers[ActiveModifier]).CanADS;
        public Transform ShotSource;
        [Header("Recoil")]
        [SerializeField] private float Recoil;
        public float MovementError;
        [Header("Reload")]
        [SerializeField] private float ShotReloadTime;
        public override int MagSize => ((RifleItemModifier)Modifiers[ActiveModifier]).MagSize;

        public override float ReloadTime => ShotReloadTime;
        protected override Vector3 ADSPos => ((RifleItemModifier)Modifiers[ActiveModifier]).ADSPos;

        private bool _shot;
        public override void Deactivate()
        {
            base.Deactivate();

            _shot = false;
        }
        public override void StopPrimaryAction()
        {
            _shot = false;
        }

        public override void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState)
        {
            base.UpdateItem(weaponInputState, playerState);

            // first shot shooting
            if (weaponInputState.PrimaryAction && !InADSTransit && !_shot && !IsReloading)
            {
                if (BulletsLoaded > 0 && ((RifleItemModifier)Modifiers[ActiveModifier]).Shot(weaponInputState, playerState))
                {
                    BulletsLoaded--;
                    _recoil += Recoil;
                }
                _shot = true;
            }
            ((RifleItemModifier)Modifiers[ActiveModifier]).UpdateItem(InADS);
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
        
        public abstract bool Shot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState);
        public virtual void UpdateItem(bool ADS) { }
    }
}
