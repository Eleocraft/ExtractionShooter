using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExoplanetStudios.ExtractionShooter
{
    public enum ItemSlot { None, MainSlot, SecondarySlot, Utility }
    public class PlayerInventory : NetworkBehaviour
    {
        [SerializeField] private GlobalInputs GI;
        [SerializeField] private Color UnSelectedColor;
        [SerializeField] private Color SelectedColor;
        private FirstPersonController _firstPersonController;
        private Transform _itemParent;
        private InputMaster _controls;
        private Dictionary<ItemSlot, ItemObject> _itemObjects = new();
        private Dictionary<ItemSlot, Image> _itemDisplays; // OwnerOnly

        private NetworkVariable<ItemSlot> ActiveSlot = new();

        public ItemObject ActiveItemObject => _itemObjects.ContainsKey(ActiveSlot.Value) ? _itemObjects[ActiveSlot.Value] : null;
        private void Awake()
        {
            _firstPersonController = GetComponent<FirstPersonController>();
            _itemParent = _firstPersonController.PlayerModel.WeaponTransform;
            
            _itemDisplays = new();
            foreach (ItemSlot slot in Utility.GetEnumValues<ItemSlot>())
            {
                if (slot == ItemSlot.None) continue;

                if (InventoryInterface.Instantiated())
                    _itemDisplays.Add(slot, InventoryInterface.Instance.InventorySlots[slot].GetComponent<Image>());
            }

        }
        private void Start()
        {
            if (IsOwner)
            {
                _controls = GI.Controls;

                _controls.Inventory.MainWeaponSlot.performed += ChangeToMainWeaponSlot;
                _controls.Inventory.SecondaryWeaponSlot.performed += ChangeToSecondaryWeaponSlot;
                _controls.Inventory.UtilitySlot.performed += ChangeToUtilitySlot;
                _controls.Inventory.Unequip.performed += Unequip;
            }
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += OnClientConnected;

                SetItem(new(ItemSlot.MainSlot, "rifle"));
                SetItem(new(ItemSlot.SecondarySlot, "wheellock"));
                SetItem(new(ItemSlot.Utility, "granade"));
            }
        }
        private void OnClientConnected(ulong id)
        {
            foreach (KeyValuePair<ItemSlot, ItemObject> itemObject in _itemObjects)
                UpdateItemObjectClientRpc(new(itemObject.Key, itemObject.Value.ItemID, itemObject.Value.ActiveModifier, itemObject.Value.Ammunition));
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            ActiveSlot.OnValueChanged += ActivateItem;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            ActiveSlot.OnValueChanged -= ActivateItem;

            if (!IsOwner)
                return;

            _controls.Inventory.MainWeaponSlot.performed -= ChangeToMainWeaponSlot;
            _controls.Inventory.SecondaryWeaponSlot.performed -= ChangeToSecondaryWeaponSlot;
            _controls.Inventory.UtilitySlot.performed -= ChangeToUtilitySlot;
            _controls.Inventory.Unequip.performed -= Unequip;
        }

        private void ChangeToMainWeaponSlot(InputAction.CallbackContext ctx) => ActivateItemServerRpc(ItemSlot.MainSlot);
        private void ChangeToSecondaryWeaponSlot(InputAction.CallbackContext ctx) => ActivateItemServerRpc(ItemSlot.SecondarySlot);
        private void ChangeToUtilitySlot(InputAction.CallbackContext ctx) => ActivateItemServerRpc(ItemSlot.Utility);
        private void Unequip(InputAction.CallbackContext ctx) => ActivateItemServerRpc(ItemSlot.None);

        [ServerRpc]
        private void ActivateItemServerRpc(ItemSlot activeSlot)
        {
            ActiveSlot.Value = activeSlot;
        }
        private void ActivateItem(ItemSlot oldSlot, ItemSlot newSlot)
        {
            if (newSlot != ItemSlot.None && !_itemObjects.ContainsKey(newSlot))
                return; // no item
            
            if (oldSlot == newSlot)
                return;

            // Deactivate old item
            if (_itemObjects.ContainsKey(oldSlot))
            {
                _itemObjects[oldSlot].Deactivate();
                if (IsOwner)
                    _itemDisplays[oldSlot].color = UnSelectedColor;
            }

            // Activate new item
            if (_itemObjects.ContainsKey(newSlot))
            {
                _itemObjects[newSlot].Activate();
                if (IsOwner)
                    _itemDisplays[newSlot].color = SelectedColor;
            }
        }
        
        public void SetModifier(int modifier)
        {
            SetModifierServerRpc(modifier);
        }
        [ServerRpc]
        public void SetModifierServerRpc(int modifier)
        {
            // More logic to prove validity
            SetItem(new(ActiveSlot.Value, _itemObjects[ActiveSlot.Value].ItemID, modifier, _itemObjects[ActiveSlot.Value].Ammunition));
        }
        public void SetItem(Item item)
        {
            if (!IsServer) return;

            UpdateItemObjectClientRpc(item);
        }
        [ClientRpc]
        private void UpdateItemObjectClientRpc(Item item)
        {
            if (!_itemObjects.ContainsKey(item.Slot) || _itemObjects[item.Slot].ItemID != item.Id)
                InstantiateItem(item.Slot, item.Id);
                
            _itemObjects[item.Slot].ActiveModifier = item.ActiveModifier;
                
            void InstantiateItem(ItemSlot slot, string itemId)
            {
                if (_itemObjects.ContainsKey(slot))
                {
                    if (ActiveSlot.Value == slot)
                        _itemObjects[slot].Deactivate();
                        
                    Destroy(_itemObjects[slot].gameObject);
                    _itemObjects.Remove(slot);
                }
                
                _itemObjects.Add(slot, Instantiate(ItemDatabase.Items[itemId], _itemParent));
                _itemObjects[slot].Initialize(OwnerClientId, IsOwner, _firstPersonController);
                
                if (IsOwner)
                {
                    _itemDisplays[slot].sprite = _itemObjects[slot].Icon;
                    _itemDisplays[slot].color = (ActiveSlot.Value == slot) ? SelectedColor : UnSelectedColor;
                }
                    
                if (ActiveSlot.Value == slot)
                    _itemObjects[slot].Activate();
            }
        }
        [Command]
        public static void SetWeaponModifier(List<string> args)
        {
            NetworkManager.Singleton.ConnectedClients[ulong.Parse(args[0])].PlayerObject.GetComponent<PlayerInventory>().SetModifier(int.Parse(args[2]));
        }
    }
}
