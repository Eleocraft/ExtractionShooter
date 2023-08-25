using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class Weapon : ScriptableObject
    {
        [HideInInspector] public ulong OwnerId;
        public abstract void UpdateWeapon(NetworkWeaponInputState weaponInputState, Vector3 position, Vector3 direction, float velocity);
        public virtual void StartPrimaryAction() { }
        public virtual void StopPrimaryAction() { }
        public virtual void StartSecondaryAction() { }
        public virtual void StopSecondaryAction() { }
    }
}