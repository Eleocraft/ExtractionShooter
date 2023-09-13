using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class DefaultProjectileGraphic : ProjectileGraphic
    {
        [SerializeField] private float RemainAfterEnd = 2f;
        public override void SetPositionAndDirection(Vector3 newPosition, Vector3 newDirection)
        {
            transform.position = newPosition;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, newDirection);
        }
        public override void EndProjectile()
        {
            this.Invoke(() => Destroy(gameObject), RemainAfterEnd);
        }
    }
}
