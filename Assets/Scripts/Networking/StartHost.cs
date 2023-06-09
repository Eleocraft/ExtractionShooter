using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ExoplanetStudios.ExtractionShooter
{
    public class StartHost : MonoBehaviour
    {
        void Awake()
        {
            if (NetworkManager.Singleton.SceneManager == null)
                NetworkManager.Singleton.StartHost();
            else
                Destroy(this.gameObject);
        }
        [Command]
        public static void ReloadScene(List<string> parameters)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
    }
}