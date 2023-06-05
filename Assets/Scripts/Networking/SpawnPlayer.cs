using UnityEngine;
using Unity.Netcode;

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
        NetworkObject newPlayer = Instantiate(Player, transform.position, transform.rotation);
        newPlayer.SpawnAsPlayerObject(clientId);
    }
}
