using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

// This is a wrapper for the System.Collections.Generic.Dictionary,
// which makes dictionarys with enum values as key serializable
namespace ExoplanetStudios
{
    [System.Serializable]
    public class EnumDictionary<KeyEnum, Value> where KeyEnum : System.Enum
    {
        private Dictionary<KeyEnum, Value> dict;
        [SerializeField] private List<PanelObject> values;
        public EnumDictionary()
        {
            dict = new();
            values = new();
        }
        public int Count => dict.Count;
        public KeyValuePair<KeyEnum, Value> ElementAt(int index) => dict.ElementAt(index);
        public void Update()
        {
            KeyEnum[] enumValues = Utility.GetEnumValues<KeyEnum>();
            // One field for each enumvalue
            if (values.Count != enumValues.Length)
            {
                foreach (PanelObject panelObject in values)
                    if (!enumValues.Contains(panelObject.key))
                        values.Remove(panelObject);
                foreach (KeyEnum key in enumValues)
                    if (!values.Select(v => v.key).Contains(key))
                        values.Add(new(key));
                values = values.GroupBy(x => x.key).Select(y => y.First()).ToList();
            }
            // Populating dictionary
            dict = new();
            foreach (PanelObject panel in values)
                dict.Add(panel.key, panel.value);
        }
        public Value this[KeyEnum index]
        {
            get => dict[index];
            set { dict[index] = value; }
        }
        [System.Serializable]
        private class PanelObject
        {
            [HideInInspector] public string name = "panel";
            [HideInInspector] public KeyEnum key;
            public Value value;
            public PanelObject(KeyEnum key)
            {
                this.key = key;
                name = key.ToString();
            }
        }
    }
}