using UnityEngine;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ItemDatabase : MonoBehaviour
    {
        [SerializeField] private List<ItemObject> ItemObjects;
        public static Dictionary<string, ItemObject> Items { get; private set; }

        private void Awake()
        {
#if UNITY_EDITOR
            GetItems();
#endif
            Items = new();
            for (int i = 0; i < ItemObjects.Count; i++)
            {
                if (ItemObjects[i] == null)
                {
                    ItemObjects.RemoveAt(i);
                    i--;
                    continue;
                }
                Items.Add(ItemObjects[i].ItemID, ItemObjects[i]);
            }
        }
        public void OnValidate()
        {
            for (int i = 0; i < ItemObjects.Count; i++)
                if (ItemObjects[i] == null)
                    ItemObjects.RemoveAt(i);
        }
#if UNITY_EDITOR
        [ContextMenu("Update")]
        public void GetItems()
        {
            ItemObjects ??= new();

            foreach (ItemObject i in Utility.FindAssetsByType<ItemObject>())
                if (!ItemObjects.Contains(i))
                    ItemObjects.Add(i);
                    
            OnValidate();
        }
#endif
    }
}
