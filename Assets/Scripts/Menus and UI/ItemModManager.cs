using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ItemModManager : NetworkBehaviour
    {
        [SerializeField] private GlobalInputs GI;
        [SerializeField] private Camera Cam;
        [SerializeField] private Button PlusIconPrefab;
        [SerializeField] private GameObject Panel;
        [SerializeField] private TMP_Text Description;
        [SerializeField] private GameObject DescriptionPanel;
        private List<GameObject> _plusIcons = new();
        private List<ItemModifier> _modifiers;
        private bool _active;
        private int _activeMod;
        private PlayerInventory _inventory;
        private void Start() {
            GI.Controls.Inventory.ModificationManager.performed += ToggleMenu;
        }
        private void Update() {
            if (!_active) return;

            for (int i = 0; i < _modifiers.Count; i++)
                _plusIcons[i].transform.position = Cam.WorldToScreenPoint(_modifiers[i].transform.position);
        }
        private void ToggleMenu(UnityEngine.InputSystem.InputAction.CallbackContext ctx) {
            
            _inventory = NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerInventory>();
            _active = !_active;
            Panel.SetActive(_active);
            if (_active)
            {
                _modifiers = _inventory.ActiveItemObject.Modifiers;
                for (int i = 0; i < _modifiers.Count; i++)
                {
                    Button plusIcon = Instantiate(PlusIconPrefab, Panel.transform);
                    plusIcon.onClick.AddListener(() => ActivateModifier(i));
                    _plusIcons.Add(plusIcon.gameObject);
                    
                }
            }
            else
            {
                foreach(GameObject obj in _plusIcons)
                    Destroy(obj);
                _plusIcons = new();
                _modifiers = new();
            }
        }
        private void ActivateModifier(int ID)
        {
            _activeMod = ID;
            DescriptionPanel.SetActive(true);
            Description.text = _modifiers[_activeMod].Description;
        }
        public void Apply() {
            _inventory.SetModifier(_activeMod);
        }
    }
}
