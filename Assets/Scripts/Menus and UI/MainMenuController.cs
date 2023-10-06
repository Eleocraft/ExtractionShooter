using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro;
using Netcode.Transports.Facepunch;
using Steamworks.Data;
using Steamworks;

public class MainMenuController : MonoBehaviour
{
    private const string localhost = "127.0.0.1";
    [SerializeField] private TMP_InputField IpInputField;
    [SerializeField] private string mainSceneName;
    public static bool UsedMainMenu;
    public static Lobby? MainLobby;
    void Start()
    {
        UsedMainMenu = true;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }
    void OnDestroy()
    {
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
    }
    public async void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
        MainLobby = await SteamMatchmaking.CreateLobbyAsync(5);
    }
    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
    {
        NetworkManager.Singleton.GetComponent<FacepunchTransport>().targetSteamId = steamId;
        NetworkManager.Singleton.StartClient();
    }
    public void StartClient()
    {
        // NetworkManager.Singleton.GetComponent<FacepunchTransport>().targetSteamId = ;
        // NetworkManager.Singleton.StartClient();
    }
    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
    }
    // private string GetIPfromURL(string URL)
    // {
    //     if (string.IsNullOrEmpty(URL))
    //         return localhost;
    //     if (!URL.Any(x => !char.IsLetter(x)))
    //         return URL;
    //     try 
    //     {
    //         IPHostEntry Hosts = Dns.GetHostEntry(URL);
    //         return Hosts.AddressList[0].ToString();
    //     }
    //     catch { throw new WrongAdressException(); }
    // }
    public class WrongAdressException : System.Exception
    {
        public WrongAdressException() {}
    }
}
