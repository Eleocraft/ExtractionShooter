using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using System.Net;
using TMPro;
using System.Linq;

public class MainMenuController : MonoBehaviour
{
    private const string localhost = "127.0.0.1";
    [SerializeField] private TMP_InputField IpInputField;
    [SerializeField] private string mainSceneName;
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
    }
    public void StartClient()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = GetIPfromURL(IpInputField.text);
        NetworkManager.Singleton.StartClient();
    }
    private string GetIPfromURL(string URL)
    {
        if (string.IsNullOrEmpty(URL))
            return localhost;
        if (!URL.Any(x => !char.IsLetter(x)))
            return URL;
        try 
        {
            IPHostEntry Hosts = Dns.GetHostEntry(URL);
            return Hosts.AddressList[0].ToString();
        }
        catch { throw new WrongAdressException(); }
    }
    public class WrongAdressException : System.Exception
    {
        public WrongAdressException() {}
    }
}
