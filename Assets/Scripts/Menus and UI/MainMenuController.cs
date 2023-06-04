using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using System.Net;
using TMPro;
using System.Linq;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_InputField IpInputField;
    [SerializeField] private string mainSceneName;
    public void StartHost()
    {
        Destroy(NetworkManager.Singleton.gameObject);
        SceneManager.LoadScene(mainSceneName);
    }
    public void StartClient()
    {
        if (string.IsNullOrEmpty(IpInputField.text))
            IpInputField.text = "localhost";

        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = GetIPfromURL(IpInputField.text);
        NetworkManager.Singleton.StartClient();
    }
    private string GetIPfromURL(string URL)
    {
        if (!URL.Any(x => !char.IsLetter(x)))
            return URL;
        if (URL == "localhost")
            return "127.0.0.1";
        try 
        {
            IPHostEntry Hosts = Dns.GetHostEntry(URL);
            return Hosts.AddressList[0].ToString();
        }
        catch
        {
            throw new WrongAdressException();
        }
    }
    public class WrongAdressException : System.Exception
    {
        public WrongAdressException() {}
    }
}
