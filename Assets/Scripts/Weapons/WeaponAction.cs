using UnityEngine;
using UnityEngine.InputSystem;

namespace ExoplanetStudios.ExtractionShooter
{
    public class WeaponAction : MonoBehaviour
    {
        [SerializeField] private GlobalInputs GI;
        [SerializeField] private Transform CameraSocket;
        [SerializeField] private Weapon Weapon;
        private void Start()
        {
            GI.Controls.Player.MainAction.started += StartMainAction;
            GI.Controls.Player.SecondaryAction.started += StartSecondaryAction;
            GI.Controls.Player.MainAction.canceled += StopSecondaryAction;
            GI.Controls.Player.SecondaryAction.canceled += StopSecondaryAction;
        }
        private void OnDestroy()
        {
            GI.Controls.Player.MainAction.started -= StartMainAction;
            GI.Controls.Player.SecondaryAction.started -= StartSecondaryAction;
            GI.Controls.Player.MainAction.canceled -= StopSecondaryAction;
            GI.Controls.Player.SecondaryAction.canceled -= StopSecondaryAction;
        }
        private void StartMainAction(InputAction.CallbackContext ctx) => Weapon.StartMainAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
        private void StopMainAction(InputAction.CallbackContext ctx) => Weapon.StopMainAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
        private void StartSecondaryAction(InputAction.CallbackContext ctx) => Weapon.StartSecondaryAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
        private void StopSecondaryAction(InputAction.CallbackContext ctx) => Weapon.StopSecondaryAction(CameraSocket.position, CameraSocket.rotation * Vector3.forward);
        private void Update()
        {
            Weapon.UpdateWeapon(CameraSocket.position, CameraSocket.forward);
        }
    }
}