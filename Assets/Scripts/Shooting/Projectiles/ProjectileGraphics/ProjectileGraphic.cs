using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class ProjectileGraphic : MonoBehaviour
    {
        public abstract void SetPositionAndDirection(Vector3 newPosition, Vector3 newDirection);
        public virtual void AddHit(Vector3 hitPosition, Vector3 hitDirection) => SetPositionAndDirection(hitPosition, hitDirection);
        public virtual void OnInitialisation(Vector3 position, Vector3 direction, Vector3 offset) => SetPositionAndDirection(position, direction);
        public abstract void EndProjectile();
    }
}
