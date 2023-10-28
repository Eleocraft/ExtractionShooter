using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Scoreboard : NetworkSingleton<Scoreboard>
    {
        [SerializeField] private ScoreboardItem Prefab;
        [SerializeField] private Transform ScoreboardParent;
        private static Dictionary<ulong, string> _names;
        private static Dictionary<ulong, ScoreboardItem> _scoreboardItems;
        private void Start()
        {
            foreach(KeyValuePair<ulong, string> player in _names)
            {
                ScoreboardItem item = Instantiate(Prefab, ScoreboardParent);
                item.SetName(player.Value);
                _scoreboardItems.Add(player.Key, item);
            }
        }
        public static void AddKill(ulong killer, ulong killed)
        {
            if (Instantiated())
                Instance.AddKillClientRpc(killer, killed);
        }
        [ClientRpc]
        private void AddKillClientRpc(ulong killer, ulong killed)
        {
            if (_scoreboardItems.ContainsKey(killer))
                _scoreboardItems[killer].AddKill();
                
            if (_scoreboardItems.ContainsKey(killed))
                _scoreboardItems[killed].AddDeath();
        }
        public static void SetNames(Dictionary<ulong, string> names) => _names = names;
    }
}