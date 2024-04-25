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
        
    
        public static Lobby? MainLobby;
        private Dictionary<ulong, Playercard> _playerCards = new();
        private Dictionary<ulong, string> _players = new();
        private List<Friendcard> _friendList = new();
        private SteamId _receivedInviteId;
        void Start()
        {
            Application.targetFrameRate = 60;
            NetworkManager.OnClientDisconnectCallback += OnServerLeave;

            SteamMatchmaking.OnLobbyInvite += OnGameLobbyJoinRequested;
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        }
        public override void OnDestroy()
        {
            if (NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= OnServerLeave;

            SteamMatchmaking.OnLobbyInvite -= OnGameLobbyJoinRequested;
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        }
        public async void StartHost()
        {
            NetworkManager.StartHost();
            MainPanel.SetActive(false);
            MainLobby = await SteamMatchmaking.CreateLobbyAsync(5);
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
            _players = new();
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
        [ServerRpc(RequireOwnership = false)]
        private void AddNameServerRpc(ulong id, string name)
        {
            _players.Add(id, name);
            Scoreboard.SetNames(_players);
            foreach (KeyValuePair<ulong, string> current in _players)
                AddNameClientRpc(current.Key, current.Value);
        }
        [ClientRpc]
        private void AddNameClientRpc(ulong id, string name)
        {
            if (_playerCards.ContainsKey(id)) return;

            Playercard card = Instantiate(CardPrefab, CardParent);
            card.SetName(name);
            _playerCards.Add(id, card);

            if (IsServer) return;

            _players.Add(id, name);
            Scoreboard.SetNames(_players);
        }
        
        private void OnServerLeave(ulong id)
        {
            if (_players.ContainsKey(id))
                _players.Remove(id);

            RemoveNameClientRpc(id);
            Scoreboard.SetNames(_players);
        }
        [ClientRpc]
        private void RemoveNameClientRpc(ulong id)
        {
            if (_players.ContainsKey(id))
                _players.Remove(id);
            if (_playerCards.ContainsKey(id))
            {
                Destroy(_playerCards[id].gameObject);
                _playerCards.Remove(id);
            }
            Scoreboard.SetNames(_players);
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
            this.Invoke(() => AddNameServerRpc(NetworkManager.LocalClientId, SteamClient.Name), 2f);
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