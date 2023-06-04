using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
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
}