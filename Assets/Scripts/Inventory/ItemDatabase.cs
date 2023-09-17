using UnityEngine;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ItemDatabase : MonoBehaviour
    {
        [SerializeField] private List<Weapon> WeaponObjects;
        [SerializeField] private List<UtilityItem> UtilityObjects;
        public static Dictionary<string, Weapon> WeaponItems { get; private set; }
        public static Dictionary<string, UtilityItem> UtilityItems { get; private set; }

        public void Awake()
        {
#if UNITY_EDITOR
            GetItems();
#endif
            // Weapons
            WeaponItems = new();
            for (int i = 0; i < WeaponObjects.Count; i++)
            {
                if (WeaponObjects[i] == null)
                {
                    WeaponObjects.RemoveAt(i);
                    i--;
                    continue;
                }
                WeaponItems.Add(WeaponObjects[i].name, WeaponObjects[i]);
            }
            // Utility
            UtilityItems = new();
            for (int i = 0; i < UtilityObjects.Count; i++)
            {
                if (UtilityObjects[i] == null)
                {
                    UtilityObjects.RemoveAt(i);
                    i--;
                    continue;
                }
                UtilityItems.Add(UtilityObjects[i].name, UtilityObjects[i]);
            }
        }
        public void OnValidate()
        {
            // Weapons
            for (int i = 0; i < WeaponObjects.Count; i++)
                if (WeaponObjects[i] == null)
                    WeaponObjects.RemoveAt(i);
            // Utility
            for (int i = 0; i < UtilityObjects.Count; i++)
                if (UtilityObjects[i] == null)
                    UtilityObjects.RemoveAt(i);
        }
#if UNITY_EDITOR
        [ContextMenu("Update")]
        public void GetItems()
        {
            WeaponObjects ??= new();

            foreach (Weapon i in Utility.FindAssetsByType<Weapon>())
                if (!WeaponObjects.Contains(i))
                    WeaponObjects.Add(i);

            UtilityObjects ??= new();

            foreach (UtilityItem i in Utility.FindAssetsByType<UtilityItem>())
                if (!UtilityObjects.Contains(i))
                    UtilityObjects.Add(i);
                    
            OnValidate();
        }
#endif
    }
}
