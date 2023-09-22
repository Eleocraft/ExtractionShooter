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

        public ItemObject ActiveItemObject => _itemObjects.ContainsKey(ActiveSlot.Value) ? _itemObjects[ActiveSlot.Value] : null;
        public event Action<ItemObject> ChangedActiveItem;
        private void Awake()
        {
            _firstPersonController = GetComponent<FirstPersonController>();
        }
        private void Start()
        {
            _controls = GI.Controls;
            
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += OnClientConnected;

                SetItem(new(ItemSlot.MainSlot, "wheellock"));
                SetItem(new(ItemSlot.SecondarySlot, "automatic"));
                SetItem(new(ItemSlot.Utility, "granade"));
            }
            if (IsOwner)
            {
                _controls.Inventory.MainWeaponSlot.performed += ChangeToMainWeaponSlot;
                _controls.Inventory.SecondaryWeaponSlot.performed += ChangeToSecondaryWeaponSlot;
                _controls.Inventory.UtilitySlot.performed += ChangeToUtilitySlot;
                _controls.Inventory.Unequip.performed += Unequip;
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
                _itemObjects[oldSlot].Deactivate();

            // Activate new item
            if (_itemObjects.ContainsKey(newSlot))
                _itemObjects[newSlot].Activate();

            ChangedActiveItem?.Invoke(_itemObjects[newSlot]);
        }
        
        public void SetModifier(ItemSlot itemSlot, int modifier)
        {
            if (!IsServer) return;

            SetItem(new(itemSlot, _itemObjects[itemSlot].ItemID, modifier, _itemObjects[itemSlot].Ammunition));
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
                if (!_itemObjects.ContainsKey(slot))
                    _itemObjects.Add(slot, default);
                else
                {
                    if (ActiveSlot.Value == slot)
                        _itemObjects[slot].Deactivate();
                        
                    Destroy(_itemObjects[slot]);
                }
                    
                _itemObjects[slot] = Instantiate(ItemDatabase.Items[itemId]);
                _itemObjects[slot].Initialize(OwnerClientId, IsOwner, _firstPersonController);
                    
                if (ActiveSlot.Value == slot)
                    _itemObjects[slot].Activate();
            }
        }
        [Command]
        public static void SetWeaponModifier(List<string> args)
        {
            NetworkManager.Singleton.ConnectedClients[ulong.Parse(args[0])].PlayerObject.GetComponent<PlayerInventory>().SetModifier(Enum.Parse<ItemSlot>(args[1]), int.Parse(args[2]));
        }
    }
}
