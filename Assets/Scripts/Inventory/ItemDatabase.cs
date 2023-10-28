using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ItemDatabase : MonoBehaviour
    {
        [SerializeField] private List<ItemObject> ItemObjects;
        public static Dictionary<string, ItemObject> Items { get; private set; }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex == 0)
                DontDestroyOnLoad(this);
            
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
    }
}
