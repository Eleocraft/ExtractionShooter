using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class ProjectileGraphic : MonoBehaviour
    {
        public abstract void SetPositionAndDirection(Vector3 newPosition, Vector3 newDirection);
        public abstract void EndProjectile();
    }
}
