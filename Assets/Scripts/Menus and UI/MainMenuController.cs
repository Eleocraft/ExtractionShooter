using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using TMPro;

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
        if (string.IsNullOrEmpty(IpInputField.text)) return;

        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = IpInputField.text;
        NetworkManager.Singleton.StartClient();
    }
}
