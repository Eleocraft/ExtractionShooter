using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class Weapon : ScriptableObject
    {
        public abstract void UpdateWeapon(Vector3 position, Vector3 direction);
        public abstract void StartMainAction(Vector3 position, Vector3 direction);
        public virtual void StopMainAction() { }
        public virtual void StartSecondaryAction(Vector3 position, Vector3 direction) { }
        public virtual void StopSecondaryAction() { }
    }
}