using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private Slider sensSlider;
        private FirstPersonController controller;
        private void Start()
        {
            EscQueue.pauseMenu += ToggleMenu;
        }
        private void OnDestroy()
        {
            EscQueue.pauseMenu -= ToggleMenu;
        }
        private void ToggleMenu()
        {
            pauseMenu.SetActive(!pauseMenu.activeSelf);
            CursorStateMachine.ChangeCursorState(!pauseMenu.activeSelf, this);
        }
        public void BackToMainMenu()
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
            SceneManager.LoadScene(0);
        }
        public void SetSens()
        {
            if (controller == null)
                controller = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<FirstPersonController>();
                
            controller.RotationSensitivity = sensSlider.value;
        }
    }
}