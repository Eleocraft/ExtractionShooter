using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro;
using Netcode.Transports.Facepunch;
using Steamworks.Data;
using Steamworks;
using System.Collections.Generic;
using System.Linq;

namespace ExoplanetStudios.ExtractionShooter
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string MainSceneName;
        [Header("PlayerCards")]
        [SerializeField] private Playercard CardPrefab;
        [SerializeField] private Transform CardParent;
        [Header("InviteList")]
        [SerializeField] private Friendcard FriendCard;
        [SerializeField] private Transform FriendParent;
        [SerializeField] private GameObject AcceptPanel;
        [SerializeField] private TMP_Text AcceptPanelName;
        
        [Header("Panels")]
        [SerializeField] private GameObject MainPanel;
        [SerializeField] private GameObject HostPanel;
        [SerializeField] private GameObject ClientPanel;
        [SerializeField] private GameObject InvitePanel;
        
    
        public static bool UsedMainMenu;
        public static Lobby? MainLobby;
        private Dictionary<ulong, Playercard> _joinedPlayers = new();
        private List<Friendcard> _friendList = new();
        private SteamId _receivedInviteId;
        void Start()
        {
            UsedMainMenu = true;
            SteamMatchmaking.OnLobbyInvite += OnGameLobbyJoinRequested;
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientJoin;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientLeave;
        }
        void OnDestroy()
        {
            SteamMatchmaking.OnLobbyInvite -= OnGameLobbyJoinRequested;
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;

            if (NetworkManager.Singleton != null) {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientJoin;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientLeave;
            }
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
                Destroy(card.gameObject);
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
            if (!MainLobby.HasValue) return;

            InvitePanel.SetActive(true);
            foreach(Friend friend in SteamFriends.GetFriends()) {
                _friendList.Add(Instantiate(FriendCard, FriendParent));
                _friendList.Last().Initialize(friend.Name, () => InviteFriend(friend.Id));
            }
        }
        public void CloseInviteList()
        {
            InvitePanel.SetActive(false);
            foreach(Friendcard card in _friendList) {
                Destroy(card.gameObject);
            }
            _friendList = new();
        }
        public void InviteFriend(SteamId steamId)
        {
            CloseInviteList();
            MainLobby.Value.InviteFriend(steamId);
        }
        public void OnClientJoin(ulong id)
        {
            if (!MainLobby.HasValue) { // If the lobby hasn't been fully created, try again after 1 second
                this.Invoke(() => OnClientJoin(id), 1);
                return;
            }

            _joinedPlayers.Add(id, Instantiate(CardPrefab, CardParent));
            int i = 0;
            foreach(Friend friend in MainLobby.Value.Members) { // Update all namecards
                _joinedPlayers.ElementAt(i).Value.SetName(friend.Name);
                i++;
            }
        }
        public void OnClientLeave(ulong id)
        {
            Destroy(_joinedPlayers[id].gameObject);
            _joinedPlayers.Remove(id);
        }
        private void OnGameLobbyJoinRequested(Friend friend, Lobby lobby)
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient) return;
            
            AcceptPanel.SetActive(true);
            AcceptPanelName.text = friend.Name;
            _receivedInviteId = friend.Id;
        }
        public void AcceptInvite()
        {
            AcceptPanel.SetActive(false);
            JoinLobby();
        }
        public void DeclineInvite() => AcceptPanel.SetActive(false);
        private void JoinLobby()
        {
            NetworkManager.Singleton.GetComponent<FacepunchTransport>().targetSteamId = _receivedInviteId;
            NetworkManager.Singleton.StartClient();
            MainPanel.SetActive(false);
            ClientPanel.SetActive(true);
        }
        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            if(result != Result.OK)
            {
                Debug.LogError($"Lobby couldn't be created!,{result}",this);
                Back();
                return;
            }
            lobby.SetFriendsOnly();
            lobby.SetData("name", "Extraction Shooter lobby");
            lobby.SetJoinable(true);
        }
        public void StartGame()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            NetworkManager.Singleton.SceneManager.LoadScene(MainSceneName, LoadSceneMode.Single);
        }
    }
}