using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace ExoplanetStudios.ExtractionShooter
{
    public enum ItemSlot { None, MainSlot, SecondarySlot, Utility }
    public class PlayerInventory : NetworkBehaviour
    {
        [SerializeField] private GlobalInputs GI;
        private FirstPersonController _firstPersonController;
        private InputMaster _controls;
        private Dictionary<ItemSlot, ItemObject> _items = new();

        private NetworkVariable<ItemSlot> ActiveSlot = new();

        public ItemObject ActiveItemObject => ActiveSlot.Value == ItemSlot.None ? null : _items[ActiveSlot.Value];
        public event Action<ItemObject> ChangedActiveItem;
        private void Start()
        {
            _controls = GI.Controls;
            _firstPersonController = GetComponent<FirstPersonController>();

            // Temp
            InstantiateItem(ItemSlot.MainSlot, ItemDatabase.Items["wheellock"]);
            InstantiateItem(ItemSlot.SecondarySlot, ItemDatabase.Items["automatic"]);
            InstantiateItem(ItemSlot.Utility, ItemDatabase.Items["granade"]);

            ActiveSlot.OnValueChanged += ActivateItem;

            if (!IsOwner)
                return;

            _controls.Inventory.MainWeaponSlot.performed += ChangeToMainWeaponSlot;
            _controls.Inventory.SecondaryWeaponSlot.performed += ChangeToSecondaryWeaponSlot;
            _controls.Inventory.UtilitySlot.performed += ChangeToUtilitySlot;
            _controls.Inventory.Unequip.performed += Unequip;
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
        private void InstantiateItem(ItemSlot slot, ItemObject itemObject)
        { // Temp
            _items.Add(slot, Instantiate(itemObject));
            _items[slot].Initialize(OwnerClientId, IsOwner, _firstPersonController);
            
            if (ActiveSlot.Value == slot)
                _items[slot].Activate();
        }
        [ServerRpc]
        private void ActivateItemServerRpc(ItemSlot activeSlot)
        {
            ActiveSlot.Value = activeSlot;
        }
        private void ActivateItem(ItemSlot oldSlot, ItemSlot newSlot)
        {
            if (newSlot != ItemSlot.None && !_items.ContainsKey(newSlot))
                return; // no item
            
            if (oldSlot == newSlot)
                return;

            // Deactivate old item
            if (_items.ContainsKey(oldSlot))
                _items[oldSlot].Deactivate();

            // Activate new item
            if (_items.ContainsKey(newSlot))
                _items[newSlot].Activate();

            ChangedActiveItem?.Invoke(_items[newSlot]);
        }
    }
}
