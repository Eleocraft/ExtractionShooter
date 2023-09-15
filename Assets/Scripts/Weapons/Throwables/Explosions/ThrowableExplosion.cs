using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class ThrowableExplosion : MonoBehaviour
    {
        public abstract void ExecuteExplosion(ulong ownerId, int tickDiff);
    }
}
