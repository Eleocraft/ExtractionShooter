

using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class InventoryInterface : MonoSingleton<InventoryInterface>
    {
        public EnumDictionary<ItemSlot, GameObject> InventorySlots;
        protected override void SingletonAwake()
        {
            InventorySlots.Update();
        }
        private void OnValidate()
        {
            InventorySlots.Update();
        }
    }
}
