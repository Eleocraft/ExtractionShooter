using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class Weapon : ScriptableObject
    {
        [HideInInspector] public ulong OwnerId;
        public abstract void UpdateWeapon(Vector3 position, Vector3 direction);
        public abstract void StartMainAction();
        public virtual void StopMainAction() { }
        public virtual void StartSecondaryAction() { }
        public virtual void StopSecondaryAction() { }
    }
}