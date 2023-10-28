using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngineInternal;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private Slider sensSlider;
        [SerializeField] private GlobalInputs GI;
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
            GI.Reset();
        }
        public void SetSens()
        {
            if (controller == null)
                controller = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<FirstPersonController>();
                
            controller.RotationSensitivity = sensSlider.value;
        }
    }
}