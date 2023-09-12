using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class NetworkTransformState : NetworkState, INetworkSerializable
    {
        public Vector3 Position;
        public Vector2 LookRotation;
        public Vector3 Velocity;
        public bool Crouch;
        public bool Predicted;

        public NetworkTransformState() {}
        public NetworkTransformState(int tick)
        {
            Tick = tick;
        }
        public NetworkTransformState(NetworkTransformState oldState, int tick)
        {
            Position = oldState.Position;
            LookRotation = oldState.LookRotation;
            Velocity = oldState.Velocity;
            Predicted = oldState.Predicted;
            Crouch = oldState.Crouch;
            
            Tick = tick;
        }
        public NetworkTransformState(int tick, Vector3 position, Vector2 lookRotation, Vector3 veloctiy, bool crouch)
        {
            Tick = tick;
            Position = position;
            LookRotation = lookRotation;
            Velocity = veloctiy;
            Crouch = crouch;
        }
        public override NetworkState GetStateWithTick(int tick) => new NetworkTransformState(this, tick);
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Tick);
                reader.ReadValueSafe(out Position);
                reader.ReadValueSafe(out LookRotation);
                reader.ReadValueSafe(out Velocity);
                reader.ReadValueSafe(out Crouch);
                reader.ReadValueSafe(out Predicted);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Tick);
                writer.WriteValueSafe(Position);
                writer.WriteValueSafe(LookRotation);
                writer.WriteValueSafe(Velocity);
                writer.WriteValueSafe(Crouch);
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
                Velocity == otherState.Velocity && Crouch == otherState.Crouch && Predicted == otherState.Predicted)
                return true;

            return false;
        }
        public override int GetHashCode()
        {
            return (int)Position.x + (int)Position.y + (int)LookRotation.x + (int)LookRotation.y;
        }
    }
    public class NetworkTransformStateList : NetworkStateList<NetworkTransformState>, INetworkSerializable
    {
		public NetworkTransformStateList() { }
        public NetworkTransformStateList(int ticksSaved)
        {
            _ticksSaved = ticksSaved;
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
                reader.ReadValueSafe(out _lastReceivedTick);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(States.Count);
                for (int i = 0; i < States.Count; i++)
                    writer.WriteValueSafe(States[i]);

                writer.WriteValueSafe(_ticksSaved);
                writer.WriteValueSafe(_lastReceivedTick);
            }
        }
    }
}