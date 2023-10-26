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
        private List<Playercard> _playerCards = new();
        private List<Friendcard> _friendList = new();
        private SteamId _receivedInviteId;
        private float UpdateTimer;
        private const float UPDATE_TIME = 3f;
        void Start()
        {
            UsedMainMenu = true;
            SteamMatchmaking.OnLobbyInvite += OnGameLobbyJoinRequested;
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        }
        void OnDestroy()
        {
            SteamMatchmaking.OnLobbyInvite -= OnGameLobbyJoinRequested;
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
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

            foreach (Playercard card in _playerCards)
                Destroy(card.gameObject);
            _playerCards = new();
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
        private void Update()
        {
            if (!MainLobby.HasValue) return;

            if (UpdateTimer < 0)
            {
                UpdateTimer = UPDATE_TIME;
                UpdatePlayerCards();
            }
            UpdateTimer -= Time.deltaTime;
        }
        public void UpdatePlayerCards()
        {
            foreach (Playercard card in _playerCards)
                Destroy(card.gameObject);
            _playerCards = new();
            foreach(Friend friend in MainLobby.Value.Members) { // Update all namecards
                _playerCards.Add(Instantiate(CardPrefab, CardParent));
                _playerCards.Last().SetName(friend.Name);
            }
        }
        private void OnGameLobbyJoinRequested(Friend friend, Lobby lobby)
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient) return;
            
            AcceptPanel.SetActive(true);
            AcceptPanelName.text = friend.Name;
            _receivedInviteId = friend.Id;
            MainLobby = lobby;
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
            lobby.SetJoinable(true);
            lobby.SetData("name", "Extraction Shooter lobby");
        }
        public void StartGame()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            NetworkManager.Singleton.SceneManager.LoadScene(MainSceneName, LoadSceneMode.Single);
        }
    }
}