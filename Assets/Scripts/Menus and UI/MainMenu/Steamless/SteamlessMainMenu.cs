using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace ExoplanetStudios.ExtractionShooter
{
    public class SteamlessMainMenu : NetworkBehaviour
    {
        [SerializeField] private string MainSceneName;
        
        [Header("Panels")]
        [SerializeField] private GameObject MainPanel;
        [SerializeField] private GameObject HostPanel;
        [SerializeField] private GameObject ClientPanel;
        

        void Start()
        {
            Application.targetFrameRate = 60;
        }
        public void StartHost()
        {
            NetworkManager.StartHost();
            MainPanel.SetActive(false);
            HostPanel.SetActive(true);
        }
        public void StartClient()
        {
            NetworkManager.StartClient();
            MainPanel.SetActive(false);
            ClientPanel.SetActive(true);
        }
        public void Back()
        {
            MainPanel.SetActive(true);
            HostPanel.SetActive(false);
            ClientPanel.SetActive(false);
            NetworkManager.Shutdown();
        }
        public void StartGame()
        {
            if (!NetworkManager.IsServer) return;
            NetworkManager.SceneManager.LoadScene(MainSceneName, LoadSceneMode.Single);
        }
    }
}