using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ScopeRifleMod : RifleItemModifier
    {
        public override int MagSize => 1;
        [SerializeField] private Camera Cam;
        [SerializeField] private GameObject ScopeDisplay;
        [SerializeField] [MinMaxRange(1, 20)] RangeSlider ScopeFOV;
        [SerializeField] private float ZoomSpeed;
        [SerializeField] private GlobalInputs GI;
        private float _zoom = 1;
        private bool _ads;
        public override Vector3 ADSPos => new Vector3(0, -0.05f, 0.25f);
        private bool _adsBlocked;
        public override bool CanADS => !_adsBlocked;

        public override void Initialize(ItemObject itemObject, FirstPersonController player, bool isOwner)
        {
            base.Initialize(itemObject, player, isOwner);
            Cam.enabled = false;
        }
        public override void Activate()
        {
            base.Activate();
            GI.Controls.Mouse.Scroll.performed += ChangeZoom;
        }
        public override void Deactivate()
        {
            base.Deactivate();
            GI.Controls.Mouse.Scroll.performed += ChangeZoom;
        }
        private void ChangeZoom(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (!_ads || !_isOwner) return;

            _zoom -= ctx.ReadValue<Vector2>().y * ZoomSpeed;
            _zoom = Mathf.Clamp01(_zoom);
            Cam.fieldOfView = ScopeFOV.Evaluate(_zoom);
        }

        public override bool Shot(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState, bool isOwner)
        {
            Rifle rifle = _itemObject as Rifle;

            float spray = weaponInputState.SecondaryAction ? SprayADS : Spray;
            Vector3 direction = rifle.GetShootDirection(playerState, spray, rifle.MovementError);
            Projectile.SpawnProjectile(Info, rifle.ShotSource.position, rifle.GetCameraPosition(playerState), direction, rifle.OwnerId, weaponInputState.TickDiff);
            if (isOwner)
                SFXSource.Source.PlayOneShot(Audio);
            else
                rifle.audioSource.PlayOneShot(Audio);
            //_adsBlocked = true;
            
            return true;
        }
        public override void UpdateItem(bool ADS)
        {
            _ads = ADS;
            
            if (_adsBlocked && !_ads)
                _adsBlocked = false;

            if (!_isOwner)
                return;

            if (ADS && !Cam.enabled)
            {
                Cam.enabled = true;
                ScopeDisplay.SetActive(true);
            }
            else if (!ADS && Cam.enabled)
            {
                Cam.enabled = false;
                ScopeDisplay.SetActive(false);
            }
        }
    }
}
