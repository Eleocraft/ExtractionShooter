using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class SpawnPlayer : NetworkBehaviour
    {
        [SerializeField] private NetworkObject Player;
        private void Start()
        {
            SpawnPlayerServerRpc(NetworkManager.LocalClientId);
        }
        [ServerRpc(RequireOwnership = false)]
        private void SpawnPlayerServerRpc(ulong clientId)
        {
            NetworkObject newPlayer = Instantiate(Player);
            newPlayer.SpawnAsPlayerObject(clientId);
            newPlayer.GetComponent<FirstPersonController>().SetPosition(SpawnPoints.GetSpawnPoint());
        }
    }
}