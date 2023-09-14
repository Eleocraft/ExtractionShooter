using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Scoreboard : NetworkBehaviour
    {
        private ScoreboardData _scores; // Serveronly
        [SerializeField] TMP_Text text;
        public static Scoreboard Instance; // Serveronly
        private void Start()
        {
            if (!IsServer)
                return;
            
            Instance = this;
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            _scores = new() {
                ScoreboardSlots = new()
                {
                    { OwnerClientId, new() }
                }
            };
        }
        private void OnClientConnected(ulong id)
        {
            _scores.ScoreboardSlots.Add(id, new());
        }
        private void OnClientDisconnected(ulong id)
        {
            _scores.ScoreboardSlots.Remove(id);
        }
        public static void AddKill(ulong killId, ulong deathId)
        {
            Instance._scores.ScoreboardSlots[killId].Kills++;
            Instance._scores.ScoreboardSlots[deathId].Deaths++;
            Instance.OnScoreboardValueChangedClientRpc(Instance._scores);
        }
        [ClientRpc]
        private void OnScoreboardValueChangedClientRpc(ScoreboardData newData)
        {
            text.text = "";
            foreach (ScoreboardItem item in newData.ScoreboardSlots.Values)
            {
                text.text += "K: " + item.Kills + " D: " + item.Deaths + "\n";
            }
        }
        [Command]
        public static void ClearScoreboard(List<string> param)
        {
            foreach (ScoreboardItem item in Instance._scores.ScoreboardSlots.Values)
            {
                item.Deaths = 0;
                item.Kills = 0;
            }
        }
    }
    public class ScoreboardData : INetworkSerializable
    {
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
