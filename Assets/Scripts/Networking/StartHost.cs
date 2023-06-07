using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace ExoplanetStudios.ExtractionShooter
{
    public class StartHost : MonoBehaviour
    {
        void Awake()
        {
            if (NetworkManager.Singleton.SceneManager == null)
            {
                NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
            }
            else
                Destroy(this.gameObject);
        }
    }
}