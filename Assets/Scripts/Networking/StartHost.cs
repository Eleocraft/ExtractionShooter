using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

namespace ExoplanetStudios.ExtractionShooter
{
    public class StartHost : MonoBehaviour
    {
        [SerializeField] private bool SimulatedDelay;
        void Awake()
        {
            if (NetworkManager.Singleton.SceneManager == null)
            {
                if (SimulatedDelay)
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetDebugSimulatorParameters(120, 5, 3);
                NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
            }
            else
                Destroy(this.gameObject);
        }
    }
}