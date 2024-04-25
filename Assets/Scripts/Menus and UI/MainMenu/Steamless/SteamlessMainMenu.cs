using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class SteamlessMainMenu : NetworkBehaviour
    {
        [SerializeField] private string MainSceneName;
        
        [Header("Panels")]
        [SerializeField] private GameObject MainPanel;
        [SerializeField] private GameObject HostPanel;
        [SerializeField] private GameObject ClientPanel;
        
    
        private Dictionary<ulong, string> _players = new();
        void Start()
        {
            Application.targetFrameRate = 60;
            NetworkManager.OnClientDisconnectCallback += OnServerLeave;
        }
        public override void OnDestroy()
        {
            if (NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= OnServerLeave;
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
        private void OnServerLeave(ulong id)
        {
            if (_players.ContainsKey(id))
                _players.Remove(id);

            Scoreboard.SetNames(_players);
        }
        public void StartGame()
        {
            if (!NetworkManager.IsServer) return;
            NetworkManager.SceneManager.LoadScene(MainSceneName, LoadSceneMode.Single);
        }
    }
}