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
        private Dictionary<ItemSlot, ItemObject> _itemObjects = new();

        private NetworkVariable<ItemSlot> ActiveSlot = new();
        private NetworkVariable<NetworkItems> Items = new();

        public ItemObject ActiveItemObject => ActiveSlot.Value == ItemSlot.None ? null : _itemObjects[ActiveSlot.Value];
        public event Action<ItemObject> ChangedActiveItem;
        private void Start()
        {
            _controls = GI.Controls;
            _firstPersonController = GetComponent<FirstPersonController>();
            ActiveSlot.OnValueChanged += ActivateItem;
            Items.OnValueChanged += UpdateActiveItems;

            if (IsServer)
            {
                Items.Value = new();
                AddItem(ItemSlot.MainSlot, "wheellock");
                AddItem(ItemSlot.SecondarySlot, "automatic");
                AddItem(ItemSlot.Utility, "granade");
            }
            if (IsOwner)
            {
                _controls.Inventory.MainWeaponSlot.performed += ChangeToMainWeaponSlot;
                _controls.Inventory.SecondaryWeaponSlot.performed += ChangeToSecondaryWeaponSlot;
                _controls.Inventory.UtilitySlot.performed += ChangeToUtilitySlot;
                _controls.Inventory.Unequip.performed += Unequip;
            }
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            ActiveSlot.OnValueChanged -= ActivateItem;
            Items.OnValueChanged -= UpdateActiveItems;

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
                _itemObjects[oldSlot].Deactivate();

            // Activate new item
            if (_itemObjects.ContainsKey(newSlot))
                _itemObjects[newSlot].Activate();

            ChangedActiveItem?.Invoke(_itemObjects[newSlot]);
        }

        public void AddItem(ItemSlot slot, string itemID)
        {
            if (!IsServer) return;

            Items.Value.Items.Add((slot, itemID));
            Items.Value = new NetworkItems { Items = Items.Value.Items };
        }
        private void UpdateActiveItems(NetworkItems old, NetworkItems newItems)
        {
            foreach ((ItemSlot slot, string id) item in newItems.Items)
                if (!_itemObjects.ContainsKey(item.slot) || _itemObjects[item.slot].ItemID != item.id)
                    InstantiateItem(item.slot, item.id);


            void InstantiateItem(ItemSlot slot, string itemID)
            {
                if (ActiveSlot.Value == slot)
                    _itemObjects[slot].Deactivate();

                if (!_itemObjects.ContainsKey(slot))
                    _itemObjects.Add(slot, default);
                else
                    Destroy(_itemObjects[slot]);
                
                _itemObjects[slot] = Instantiate(ItemDatabase.Items[itemID]);
                _itemObjects[slot].Initialize(OwnerClientId, IsOwner, _firstPersonController);
                
                if (ActiveSlot.Value == slot)
                    _itemObjects[slot].Activate();
            }
        }
        private class NetworkItems : INetworkSerializable
        {
            public List<(ItemSlot, string)> Items = new();
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                if (serializer.IsReader)
                {
                    FastBufferReader reader = serializer.GetFastBufferReader();
                    reader.ReadValueSafe(out int count);
                    Items = new();
                    for (int i = 0; i < count; i++)
                    {
                        reader.ReadValueSafe(out ItemSlot slot);
                        reader.ReadValueSafe(out string id);
                        Items.Add((slot, id));
                    }
                }
                else
                {
                    FastBufferWriter writer = serializer.GetFastBufferWriter();
                    writer.WriteValueSafe(Items.Count);
                    foreach ((ItemSlot slot, string id) item in Items)
                    {
                        writer.WriteValueSafe(item.slot);
                        writer.WriteValueSafe(item.id);
                    }
                }
            }
        }
    }
}
