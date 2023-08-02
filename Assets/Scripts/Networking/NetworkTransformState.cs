using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class NetworkTransformState : INetworkSerializable
    {
        public int Tick;
        public Vector3 Position;
        public Vector2 LookRotation;
        public Vector3 Velocity;
        public bool Predicted;

        public NetworkTransformState() {}
        public NetworkTransformState(int tick)
        {
            Tick = tick;
        }
        public NetworkTransformState(int tick, Vector3 position, Vector2 lookRotation, Vector3 veloctiy)
        {
            Tick = tick;
            Position = position;
            LookRotation = lookRotation;
            Velocity = veloctiy;
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Tick);
                reader.ReadValueSafe(out Position);
                reader.ReadValueSafe(out LookRotation);
                reader.ReadValueSafe(out Velocity);
                reader.ReadValueSafe(out Predicted);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Tick);
                writer.WriteValueSafe(Position);
                writer.WriteValueSafe(LookRotation);
                writer.WriteValueSafe(Velocity);
                writer.WriteValueSafe(Predicted);
            }
        }
        public static bool operator==(NetworkTransformState s1, NetworkTransformState s2)
        {
            if (s1 is null)
                return s2 is null;
            return s1.Equals(s2);
        }
        public static bool operator!=(NetworkTransformState s1, NetworkTransformState s2)
        {
            if (s1 is null)
                return !(s2 is null);
            return !s1.Equals(s2);
        }
        public override bool Equals(object obj)
        {
            NetworkTransformState otherState = (NetworkTransformState)obj;

            if (otherState is null)
                return false;

            if (Position == otherState.Position && LookRotation == otherState.LookRotation &&
                Velocity == otherState.Velocity && Predicted == otherState.Predicted)
                return true;

            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    public class NetworkTransformStateList : INetworkSerializable
    {
		private int _ticksSaved;
        private List<NetworkTransformState> States = new();
        public NetworkTransformState LastState => States.Count > 0 ? States[0] : null;
        public NetworkTransformState this[int tick]
        {
            get
            {
                if (LastState?.Tick - _ticksSaved > tick) // Tick is to old
                    return null;
                
                for (int i = States.Count - 1; i >= 0; i--)
                    if (States[i].Tick <= tick)
                        return States[i];

                Debug.Log(States.Count);
                return null;
            }
            set
            {
                if (LastState?.Tick - _ticksSaved > tick) // Tick is to old
                    return;
                
                for (int i = States.Count - 1; i >= 0; i--)
                {
                    if (States[i].Tick == tick)
                        States[i] = value;
                    else if (States[i].Tick < tick)
                        States.Insert(i, value);
                }
            }
        }
        public NetworkTransformStateList() { }
        public NetworkTransformStateList(int ticksSaved)
        {
            _ticksSaved = ticksSaved;
        }
        public void Add(NetworkTransformState inputState)
        {
            if (States.Count > 0 && LastState == inputState)
                States[0].Tick = inputState.Tick;
            else
                States.Insert(0, inputState);
        }
        public void RemoveOutdated()
        {
            if (States.Count == 0)
                return;
            
            int startTick = LastState.Tick;
            for (int i = States.Count - 1; i >= 0; i--)
                if (States[i].Tick < startTick - _ticksSaved)
                    States.RemoveAt(i);
        }
        public NetworkTransformStateList GetListForTicks(int ticks)
        {
            if (States.Count == 0)
                return null;
            
            NetworkTransformStateList newList = new(ticks);
            int startTick = LastState.Tick;
            for (int i = States.Count - 1; i >= 0; i--)
            {
                if (States[i].Tick < startTick - ticks)
                    break;
                
                newList.Add(States[i]);
            }
            return newList;
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int count);
                States = new();
                for (int i = 0; i < count; i++)
                {
                    reader.ReadValueSafe(out NetworkTransformState state);
                    States.Add(state);
                }
                reader.ReadValueSafe(out _ticksSaved);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(States.Count);
                for (int i = 0; i < States.Count; i++)
                    writer.WriteValueSafe(States[i]);

                writer.WriteValueSafe(_ticksSaved);
            }
        }
    }
}