

using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class InventoryInterface : MonoSingleton<InventoryInterface>
    {
        public EnumDictionary<ItemSlot, GameObject> InventorySlots;
        private void OnValidate()
        {
            InventorySlots.Update();
        }
    }
}
