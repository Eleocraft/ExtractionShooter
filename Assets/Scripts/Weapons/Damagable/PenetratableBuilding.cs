using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PenetratableBuilding : MonoBehaviour, IDamagable
    {
        [SerializeField] [Range(0.7f, 1)] private float BuildingResistance;
        public void OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ulong ownerId, ref Vector3 velocity)
        {
            Instantiate(info.PenetrateMarker, point, Quaternion.identity, transform).Initialize(normal, velocity);
            velocity -= velocity * BuildingResistance * (1 - info.PenetrationForce);
        }
        public void OnExit(ProjectileInfo info, Vector3 point, Vector3 normal, Vector3 velocity)
        {
            Instantiate(info.ExitMarker, point, Quaternion.identity, transform).Initialize(normal, velocity);
        }
    }
}