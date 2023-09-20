using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class ItemObject : ScriptableObject
    {
        protected FirstPersonController _controller;
        protected ulong _ownerId;
        protected bool _isOwner;
        [ReadOnly] public string ItemID;
        public int ActiveModifier;
        public float VelocityMultiplier;
        
        protected Transform _cameraTransform;
        public virtual void Initialize(ulong ownerId, bool isOwner, FirstPersonController controller) {
            
            _cameraTransform = controller.PlayerModel.CameraSocket;
            _controller = controller;
            _ownerId = ownerId;
            _isOwner = isOwner;
        }
        private void OnValidate()
        {
            ItemID = Utility.CreateID(name);
        }
        public virtual void Activate() {
            _controller.SetMovementSpeedMultiplier(GetInstanceID()+"ItemSlow", VelocityMultiplier);
        }
        public virtual void Deactivate() {
            _controller.SetMovementSpeedMultiplier(GetInstanceID()+"ItemSlow", 1f);
        }
        public abstract void UpdateItem(NetworkWeaponInputState weaponInputState, NetworkTransformState playerState);
        
        public virtual void StartPrimaryAction() { }
        public virtual void StopPrimaryAction() { }
        public virtual void StartSecondaryAction() { }
        public virtual void StopSecondaryAction() { }

        public virtual void Reload() { }

        protected Vector3 GetCameraPosition(NetworkTransformState playerState) => Vector3.up * _cameraTransform.localPosition.y + playerState.Position;
        protected Vector3 GetLookDirection(NetworkTransformState playerState) => Quaternion.Euler(playerState.LookRotation.x, playerState.LookRotation.y, 0) * Vector3.forward;
    }
    public struct Item : INetworkSerializable, System.IEquatable<Item>
    {
        public ItemSlot Slot;
        public string Id;
        public int ActiveModifier;

        public Item(ItemSlot slot, string id)
        {
            Slot = slot;
            Id = id;
            ActiveModifier = 0;
        }
        public Item(ItemSlot slot, string id, int activeModifier)
        {
            Slot = slot;
            Id = id;
            ActiveModifier = activeModifier;
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
