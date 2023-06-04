using UnityEngine;
using Unity.Netcode;

public class StartHost : MonoBehaviour
{
    void Start()
    {
        if (NetworkManager.Singleton.SceneManager == null)
            NetworkManager.Singleton.StartHost();
        else
            Destroy(this.gameObject);
    }
}
