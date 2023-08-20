using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class SpawnPoints : MonoBehaviour
    {
        private static Transform[] _spawnPoints;

        private const float SPAWN_POINT_RADIUS = 5f;
        private const string PLAYER_LAYER_NAME = "Player";
        void Awake()
        {
            _spawnPoints = GetComponentsInChildren<Transform>(true).Where(t => t != transform).ToArray();
        }
        public static Vector3 GetSpawnPoint()
        {
            List<Vector3> activeSpawnPoints = new();

            foreach(Transform spawnPoint in _spawnPoints)
                if (!Physics.CheckSphere(spawnPoint.position, SPAWN_POINT_RADIUS, LayerMask.GetMask(PLAYER_LAYER_NAME)))
                    activeSpawnPoints.Add(spawnPoint.position);

            if (activeSpawnPoints.Count <= 0)
            {
                Debug.LogError("All spawnPoints blocked");
                activeSpawnPoints.Add(Vector2.zero);
            }
            return activeSpawnPoints[Random.Range(0, activeSpawnPoints.Count)];
        }
    }
}
