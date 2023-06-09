using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class StartHost : MonoBehaviour
    {
        void Awake()
        {
            if (!MainMenuController.UsedMainMenu)
                NetworkManager.Singleton.StartHost();
            else
                Destroy(this.gameObject);
        }
    }
}