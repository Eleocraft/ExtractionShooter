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
    public class MainMenuController : NetworkBehaviour
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
        private Dictionary<ulong, Playercard> _playerCards = new();
        private List<Friendcard> _friendList = new();
        private SteamId _receivedInviteId;
        void Start()
        {
            UsedMainMenu = true;
            SteamMatchmaking.OnLobbyInvite += OnGameLobbyJoinRequested;
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            NetworkManager.OnClientDisconnectCallback += OnServerLeave;
        }
        public override void OnDestroy()
        {
            SteamMatchmaking.OnLobbyInvite -= OnGameLobbyJoinRequested;
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;

            if (NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= OnServerLeave;
        }
        public async void StartHost()
        {
            NetworkManager.StartHost();
            MainLobby = await SteamMatchmaking.CreateLobbyAsync(5);
            MainPanel.SetActive(false);
            HostPanel.SetActive(true);
            AddNameServerRpc(NetworkManager.LocalClientId, SteamClient.Name);
        }
        public void Back()
        {
            MainPanel.SetActive(true);
            HostPanel.SetActive(false);
            ClientPanel.SetActive(false);
            NetworkManager.Shutdown();

            foreach (Playercard card in _playerCards.Values)
                Destroy(card.gameObject);
            _playerCards = new();
            MainLobby = null;
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
        private void OnGameLobbyJoinRequested(Friend friend, Lobby lobby)
        {
            if (MainLobby.HasValue) return;
            
            AcceptPanel.SetActive(true);
            AcceptPanelName.text = friend.Name;
            _receivedInviteId = friend.Id;
            MainLobby = lobby;
        }
        [ServerRpc]
        private void AddNameServerRpc(ulong id, string name)
        {
            Playercard card = Instantiate(CardPrefab, CardParent);
            card.SetName(name);
            _playerCards.Add(id, card);
            foreach (KeyValuePair<ulong, Playercard> current in _playerCards)
                AddNameClientRpc(current.Key, current.Value.Name);
        }
        [ClientRpc]
        private void AddNameClientRpc(ulong id, string name)
        {
            if (_playerCards.ContainsKey(id)) return;

            Playercard card = Instantiate(CardPrefab, CardParent);
            card.SetName(name);
            _playerCards.Add(id, card);
        }
        
        private void OnServerLeave(ulong id)
        {
            if (!_playerCards.ContainsKey(id)) return;
                
            Destroy(_playerCards[id].gameObject);
            _playerCards.Remove(id);
            RemoveNameClientRpc(id);
        }
        [ClientRpc]
        private void RemoveNameClientRpc(ulong id)
        {
            if (!_playerCards.ContainsKey(id)) return;

            Destroy(_playerCards[id].gameObject);
            _playerCards.Remove(id);
        }
        public void AcceptInvite()
        {
            AcceptPanel.SetActive(false);
            JoinLobby();
        }
        public void DeclineInvite() => AcceptPanel.SetActive(false);
        private void JoinLobby()
        {
            NetworkManager.GetComponent<FacepunchTransport>().targetSteamId = _receivedInviteId;
            NetworkManager.StartClient();
            MainPanel.SetActive(false);
            ClientPanel.SetActive(true);
            AddNameServerRpc(NetworkManager.LocalClientId, SteamClient.Name);
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
            if (!NetworkManager.IsServer) return;
            NetworkManager.SceneManager.LoadScene(MainSceneName, LoadSceneMode.Single);
        }
    }
}