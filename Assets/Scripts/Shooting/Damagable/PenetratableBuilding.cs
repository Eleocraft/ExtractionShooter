using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PenetratableBuilding : MonoBehaviour, IProjectileTarget
    {
        [SerializeField] [Range(0.7f, 1)] private float BuildingResistance;
        public bool OnHit(ProjectileInfo info, Vector3 point, Vector3 normal, ref Vector3 velocity)
        {
            Instantiate(info.PenetrateMarker, point, Quaternion.identity, transform).Initialize(normal, velocity);
            return true;
        }
        public void OnExit(ProjectileInfo info, Vector3 point, Vector3 normal, float travel, ref Vector3 velocity)
        {
            Instantiate(info.ExitMarker, point, Quaternion.identity, transform).Initialize(normal, velocity);
            velocity -= velocity * BuildingResistance * (1 - info.PenetrationForce) * travel;
        }
    }
}