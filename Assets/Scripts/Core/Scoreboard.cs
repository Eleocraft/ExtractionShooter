using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Scoreboard : NetworkSingleton<Scoreboard>
    {
        [SerializeField] private TMP_Text textPrefab;   
        [SerializeField] private Transform Parent;   
        private ScoreboardData _scores;
        private Dictionary<ulong, TMP_Text> _textObjects = new Dictionary<ulong, TMP_Text>();
        private void Start()
        {
            _scores = new();

            if (!IsServer)
                return;
            
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            _scores.ScoreboardSlots.Add(OwnerClientId, new());
        }
        private void OnClientConnected(ulong id)
        {
            _scores.ScoreboardSlots.Add(id, new());
            Instance.OnScoreboardValueChangedClientRpc(Instance._scores);
        }
        private void OnClientDisconnected(ulong id)
        {
            _scores.ScoreboardSlots.Remove(id);
            Instance.OnScoreboardValueChangedClientRpc(Instance._scores);
        }
        private void CreateScoreboardUI()
        {
            foreach (TMP_Text text in _textObjects.Values)
                Destroy(text.gameObject);
            
            _textObjects = new();
            foreach (ulong scoreboardId in _scores.ScoreboardSlots.Keys)
                _textObjects.Add(scoreboardId, Instantiate(textPrefab, Parent));
            
            UpdateScoreboardUI();
        }
        private void UpdateScoreboardUI()
        {
            foreach (KeyValuePair<ulong, ScoreboardItem> scoreboardItem in _scores.ScoreboardSlots)
                _textObjects[scoreboardItem.Key].text = $"Player_{scoreboardItem.Key}:  K:{scoreboardItem.Value.Kills}  D:{scoreboardItem.Value.Deaths}";
        }
        public static void AddKill(ulong killId, ulong deathId)
        {
            Instance._scores.ScoreboardSlots[killId].Kills++;
            Instance._scores.ScoreboardSlots[deathId].Deaths++;
            Instance.OnScoreboardValueChangedClientRpc(Instance._scores);
        }
        [ClientRpc]
        private void OnScoreboardValueChangedClientRpc(ScoreboardData data)
        {
            if (SameDict(data.ScoreboardSlots, _textObjects))
            {
                _scores = data;
                UpdateScoreboardUI();
            }
            else
            {
                _scores = data;
                CreateScoreboardUI();
            }

            bool SameDict(Dictionary<ulong, ScoreboardItem> data, Dictionary<ulong, TMP_Text> objects)
            {
                foreach (KeyValuePair<ulong, ScoreboardItem> item in data)
                    if (!objects.ContainsKey(item.Key))
                        return false;

                foreach (KeyValuePair<ulong, TMP_Text> item in objects)
                    if (!data.ContainsKey(item.Key))
                        return false;

                return true;
            }
        }
        [Command]
        public static void ClearScoreboard(List<string> param)
        {
            if (!Instance.IsServer)
                return;
            
            foreach (ScoreboardItem item in Instance._scores.ScoreboardSlots.Values)
            {
                item.Deaths = 0;
                item.Kills = 0;
            }
            Instance.OnScoreboardValueChangedClientRpc(Instance._scores);
        }
    }
    public class ScoreboardData : INetworkSerializable
    {
        public ScoreboardData()
        {
            ScoreboardSlots = new();
        }
        public Dictionary<ulong, ScoreboardItem> ScoreboardSlots;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int count);
                ScoreboardSlots = new();
                for (int i = 0; i < count; i++)
                {
                    reader.ReadValueSafe(out ulong id);
                    reader.ReadValueSafe(out ScoreboardItem score);
                    ScoreboardSlots.Add(id, score);
                }
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(ScoreboardSlots.Count);
                foreach (KeyValuePair<ulong, ScoreboardItem> item in ScoreboardSlots)
                {
                    writer.WriteValueSafe(item.Key);
                    writer.WriteValueSafe(item.Value);
                }
            }
        }
    }
    public class ScoreboardItem : INetworkSerializable
    {
        public int Kills;
        public int Deaths;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Kills);
                reader.ReadValueSafe(out Deaths);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Kills);
                writer.WriteValueSafe(Deaths);
            }
        }
    }
}
