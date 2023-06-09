using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class HitMarker : MonoBehaviour
    {
        public abstract void Initialize(Vector3 normal, Vector3 velocity);
    }
}