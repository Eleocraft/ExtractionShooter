using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class ItemObject : MonoBehaviour
    {
        protected FirstPersonController _firstPersonController;
        [HideInInspector] public ulong OwnerId;
        protected bool _isOwner;
        [ReadOnly] public string ItemID;
        public Sprite Icon;
        [HideInInspector] public int Ammunition;
        public float VelocityMultiplier;
        protected Transform _cameraTransform;
        [Header("Modifiers")]
        private int _activeModifier;
        public List<ItemModifier> Modifiers;
        public int ActiveModifier {
            get => _activeModifier;
            set {
                if (Modifiers.Count > 0) {
                    Modifiers[_activeModifier].Deactivate();
                    Modifiers[value].Activate();
                }
                _activeModifier = value;
            }
        }
        
        public virtual void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller) {
            
            gameObject.SetActive(false);

            foreach(ItemModifier modifier in Modifiers)
                modifier.Initialize(this);

            _cameraTransform = controller.PlayerModel.CameraSocket;
            _firstPersonController = controller;
            OwnerId = ownerId;
            _isOwner = isOwner;
        }
        private void OnValidate()
        {
            ItemID = Utility.CreateID(name);
        }
        public virtual void Activate() {
            _firstPersonController.SetMovementSpeedMultiplier(GetInstanceID()+"ItemSlow", VelocityMultiplier);
            gameObject.SetActive(true);
        }
        public virtual void Deactivate() {
            _firstPersonController.SetMovementSpeedMultiplier(GetInstanceID()+"ItemSlow", 1f);
            gameObject.SetActive(false);
        }
        public abstract void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState);
        
        public virtual void StartPrimaryAction() { }
        public virtual void StopPrimaryAction() { }
        public virtual void StartSecondaryAction() { }
        public virtual void StopSecondaryAction() { }

        public virtual void Reload() { }

        public Vector3 GetCameraPosition(NetworkTransformState playerState) => Vector3.up * _cameraTransform.localPosition.y + playerState.Position;
        public Vector3 GetLookDirection(NetworkTransformState playerState) => Quaternion.Euler(playerState.LookRotation.x, playerState.LookRotation.y, 0) * Vector3.forward;
    }
    public abstract class ItemModifier : MonoBehaviour
    {
        public Vector3 Position => IconPos.position;
        public string Title;
        [TextArea] public string Description;
        public Sprite Icon;
        protected ItemObject _itemObject;
        [SerializeField] private Transform IconPos;

        public virtual void Initialize(ItemObject itemObject)
        {
            _itemObject = itemObject;
        }
        public virtual void Activate() {
            gameObject.SetActive(true);
        }
        public virtual void Deactivate() {
            gameObject.SetActive(false);
        }
    }
    public struct Item : INetworkSerializable, System.IEquatable<Item>
    {
        public ItemSlot Slot;
        public string Id;
        public int ActiveModifier;
        public int Ammunition;

        public Item(ItemSlot slot, string id)
        {
            Slot = slot;
            Id = id;
            ActiveModifier = 0;
            Ammunition = 0;
        }
        public Item(ItemSlot slot, string id, int activeModifier, int ammunition)
        {
            Slot = slot;
            Id = id;
            ActiveModifier = activeModifier;
            Ammunition = ammunition;
        }

        public bool Equals(Item other)
        {
            return Slot == other.Slot && Id == other.Id && ActiveModifier == other.ActiveModifier;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Slot);
                reader.ReadValueSafe(out Id);
                reader.ReadValueSafe(out ActiveModifier);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Slot);
                writer.WriteValueSafe(Id);
                writer.WriteValueSafe(ActiveModifier);
            }
        }
    }
}
