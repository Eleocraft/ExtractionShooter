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
        [SerializeField] private TMP_Text Title;
        [SerializeField] private GameObject DescriptionPanel;
        private List<GameObject> _plusIcons = new();
        private List<ItemModifier> _modifiers;
        private bool _active;
        private int _activeMod;
        private PlayerInventory _inventory;
        private Vector3 _weaponDefaultPos;
        private const string WEAPON_INSPECT_POS_NAME = "WeaponInspect";
        private void Start() {
            GI.Controls.Menus.ModificationManager.performed += ToggleMenuInput;
        }
        public override void OnDestroy() {
            base.OnDestroy();

            GI.Controls.Menus.ModificationManager.performed -= ToggleMenuInput;
        }
        private void Update() {
            if (!_active) return;

            for (int i = 0; i < _modifiers.Count; i++)
                _plusIcons[i].transform.position = Cam.WorldToScreenPoint(_modifiers[i].transform.position);
        }
        private void ToggleMenuInput(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => ToggleMenu();
        private void ToggleMenu() {
            
            _inventory = NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerInventory>();
            if (_inventory.ActiveItemObject == null) return;

            _active = !_active;
            Panel.SetActive(_active);
            
            if (_active)
            {
                EscQueue.Enqueue(ToggleMenu);
                InputStateMachine.ChangeInputState(false, this);
                CursorStateMachine.ChangeCursorState(false, this);
                _modifiers = _inventory.ActiveItemObject.Modifiers;
                for (int i = 0; i < _modifiers.Count; i++)
                {
                    Button plusIcon = Instantiate(PlusIconPrefab, Panel.transform);
                    int ID = i;
                    plusIcon.onClick.AddListener(() => ActivateModifier(ID));
                    plusIcon.GetComponentsInChildren<Image>()[1].sprite = _modifiers[i].Icon;
                    _plusIcons.Add(plusIcon.gameObject);
                }
                _inventory.ActiveItemObject.Deactivate();
                _inventory.ActiveItemObject.Activate();
                _weaponDefaultPos = _inventory.ActiveItemObject.transform.localPosition;
                Transform inspectPos = _inventory.ActiveItemObject.transform.parent.Find(WEAPON_INSPECT_POS_NAME);
                _inventory.ActiveItemObject.transform.position = inspectPos.position;
                _inventory.ActiveItemObject.transform.rotation = inspectPos.rotation;
            }
            else
            {
                DescriptionPanel.SetActive(false);
                EscQueue.Remove(ToggleMenu);
                InputStateMachine.ChangeInputState(true, this);
                CursorStateMachine.ChangeCursorState(true, this);
                foreach(GameObject obj in _plusIcons)
                    Destroy(obj);
                _plusIcons = new();
                _modifiers = new();
                _inventory.ActiveItemObject.transform.localPosition = _weaponDefaultPos;
                _inventory.ActiveItemObject.transform.localRotation = Quaternion.identity;
            }
        }
        private void ActivateModifier(int ID)
        {
            _activeMod = ID;
            DescriptionPanel.SetActive(true);
            Description.text = _modifiers[_activeMod].Description;
            Title.text = _modifiers[_activeMod].Title;
        }
        public void Apply() {
            DescriptionPanel.SetActive(false);
            _inventory.SetModifier(_activeMod);
        }
    }
}
