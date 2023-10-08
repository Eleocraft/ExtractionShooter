using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro;
using Netcode.Transports.Facepunch;
using Steamworks.Data;
using Steamworks;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Playercard CardPrefab;
        [SerializeField] private Transform CardParent;
        [SerializeField] private string MainSceneName;
        [Header("Panels")]
        [SerializeField] private GameObject MainPanel;
        [SerializeField] private GameObject HostPanel;
        [SerializeField] private GameObject ClientPanel;
        public static bool UsedMainMenu;
        public static Lobby? MainLobby;
        private Dictionary<ulong, Playercard> _joinedPlayers = new();
        void Start()
        {
            UsedMainMenu = true;
            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientJoin;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientLeave;
        }
        void OnDestroy()
        {
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        }
        public async void StartHost()
        {
            NetworkManager.Singleton.StartHost();
            MainLobby = await SteamMatchmaking.CreateLobbyAsync(5);
            MainPanel.SetActive(false);
            HostPanel.SetActive(true);
        }
        public void Back()
        {
            MainPanel.SetActive(true);
            HostPanel.SetActive(false);
            ClientPanel.SetActive(false);
            NetworkManager.Singleton.Shutdown();

            foreach (Playercard card in _joinedPlayers.Values)
                Destroy(card);
            _joinedPlayers = new();
        }
        public void Quit()
        {
            Application.Quit();
        }
        public void Options()
        {
            Debug.Log("What kind of options?");
        }
        public void OpenInviteList()
        {
            if (MainLobby == null) return;
            SteamFriends.OpenGameInviteOverlay(MainLobby.Value.Id);
            // MainLobby.Value.InviteFriend(76561199105991568);
        }
        public void OnClientJoin(ulong id)
        {
            _joinedPlayers.Add(id, Instantiate(CardPrefab, CardParent));
            _joinedPlayers[id].Initialize(id.ToString());
        }
        public void OnClientLeave(ulong id)
        {
            _joinedPlayers.Remove(id);
        }
        private void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
        {
            Console.Print(steamId.Value.ToString());
            NetworkManager.Singleton.GetComponent<FacepunchTransport>().targetSteamId = steamId;
            NetworkManager.Singleton.StartClient();
            MainPanel.SetActive(false);
            ClientPanel.SetActive(true);
        }
        public void StartGame()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            NetworkManager.Singleton.SceneManager.LoadScene(MainSceneName, LoadSceneMode.Single);
        }
    }
}