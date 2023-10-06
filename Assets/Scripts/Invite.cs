using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Invite : MonoBehaviour
    {
        [SerializeField] private GlobalInputs GI;
        private void Start()
        {
            GI.Controls.Menus.Invite.performed += OpenInviteList;
        }
        private void OnDestroy()
        {
            GI.Controls.Menus.Invite.performed -= OpenInviteList;
        }
        private void OpenInviteList(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            // SteamFriends.OpenGameInviteOverlay(MainMenuController.MainLobby.Value.Id);
            MainMenuController.MainLobby.Value.InviteFriend(76561199105991568);
        }
    }
}
