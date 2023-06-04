using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class StartHost : MonoBehaviour
{
    [SerializeField] private bool SimulatedDelay;
    void Start()
    {
        if (NetworkManager.Singleton.SceneManager == null)
        {
            NetworkManager.Singleton.StartHost();
            if (SimulatedDelay)
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetDebugSimulatorParameters(120, 5, 3);
        }
        else
            Destroy(this.gameObject);
    }
}
