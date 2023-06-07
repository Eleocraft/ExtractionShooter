using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// This is a wrapper for the System.Collections.Generic.Dictionary,
// which makes dictionarys serializable

namespace ExoplanetStudios
{
    [System.Serializable]
    public class SerializableDictionary<Key, Value>
    {
        private Dictionary<Key, Value> dict;
        [SerializeField] private List<PanelObject> values;
        public SerializableDictionary()
        {
            dict = new Dictionary<Key, Value>();
            values = new();
        }
        // Dictionary stuff
        public int Count => dict.Count;

        public KeyValuePair<Key, Value> ElementAt(int index) => dict.ElementAt(index);

        public Value this[Key index]
        {
            get => dict[index];
            set { dict[index] = value; }
        }
        // Display list stuff
        public void Update()
        {
            // Populating dictionary
            dict = new();
            foreach (PanelObject panel in values)
            {
                dict.Add(panel.key, panel.value);
                panel.UpdateName();
            }
        }
        public void Add(Key key) => values.Add(new(key));

        public void Remove(Key key) => values.Remove(values.Where(v => v.key.Equals(key)).Single());

        public void RemoveDublicates() => values = values.GroupBy(x => x.key).Select(y => y.First()).ToList();

        public Key[] Keys => dict.Keys.ToArray();

        [System.Serializable]
        private class PanelObject
        {
            [HideInInspector] public string name = "panel";
            public Key key;
            public Value value;
            public PanelObject(Key key)
            {
                this.key = key;
                name = key.ToString();
            }
            public void UpdateName()
            {
                name = key.ToString();
            }
        }
    }
}