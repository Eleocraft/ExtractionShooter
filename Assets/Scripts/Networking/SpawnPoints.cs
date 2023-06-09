using UnityEngine;
using System.Linq;

namespace ExoplanetStudios.ExtractionShooter
{
    public class SpawnPoints : MonoBehaviour
    {
        private static Transform[] _spawnPoints;
        void Awake()
        {
            _spawnPoints = GetComponentsInChildren<Transform>(true).Where(t => t != transform).ToArray();
        }
        public static Vector3 GetSpawnPoint()
        {
            return _spawnPoints[Random.Range(0, _spawnPoints.Length)].position;
        }
    }
}
