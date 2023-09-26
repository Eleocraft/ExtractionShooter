using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ItemModManager : NetworkBehaviour
    {
        [SerializeField] private GlobalInputs GI;
        [SerializeField] private Camera Cam;
        [SerializeField] private GameObject PlusIconPrefab;
        [SerializeField] private GameObject Panel;
        [SerializeField] private TMP_Text Description;
        private List<GameObject> _plusIcons = new();
        private List<ItemModifier> _modifiers;
        private bool _active;
        private int _activeMod;
        private PlayerInventory _inventory;
        private void Start() {
            GI.Controls.Inventory.ModificationManager.performed += ToggleMenu;
            _inventory = NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerInventory>();
        }
        private void Update() {
            if (!_active) return;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                //_plusIcons[i].transform.position = Cam.WorldToScreenPoint();
            }
        }
        private void ToggleMenu(UnityEngine.InputSystem.InputAction.CallbackContext ctx) {

            _active = !_active;
            Panel.SetActive(_active);
            if (_active)
            {
                _modifiers = _inventory.ActiveItemObject.Modifiers;
                foreach (ItemModifier itemModifier in _modifiers)
                    _plusIcons.Add(Instantiate(PlusIconPrefab, Panel.transform));
            }
            else
            {
                foreach(GameObject obj in _plusIcons)
                    Destroy(obj);
                _plusIcons = new();
                _modifiers = new();
            }
        }
        public void Apply() {
            _inventory.SetModifier(_activeMod);
        }
    }
}
