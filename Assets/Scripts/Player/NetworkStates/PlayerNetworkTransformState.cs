using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerNetworkTransformState : NetworkState, INetworkSerializable
    {
        public Vector3 Position;
        public Vector2 LookRotation;
        public Vector3 Velocity;
        public float CrouchAmount;
        public float SpeedMultiplier;

        public PlayerNetworkTransformState() {}
        public PlayerNetworkTransformState(int tick)
        {
            Tick = tick;
        }
        public PlayerNetworkTransformState(PlayerNetworkTransformState oldState, int tick)
        {
            Position = oldState.Position;
            LookRotation = oldState.LookRotation;
            Velocity = oldState.Velocity;
            CrouchAmount = oldState.CrouchAmount;
            SpeedMultiplier = oldState.SpeedMultiplier;
            
            Tick = tick;
        }
        public PlayerNetworkTransformState(int tick, Vector3 position, Vector2 lookRotation, Vector3 veloctiy, float crouch, float speedMultiplier)
        {
            Tick = tick;
            Position = position;
            LookRotation = lookRotation;
            Velocity = veloctiy;
            CrouchAmount = crouch;
            SpeedMultiplier = speedMultiplier;
        }
        public override NetworkState GetStateWithTick(int tick) => new PlayerNetworkTransformState(this, tick);
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Tick);
                reader.ReadValueSafe(out Position);
                reader.ReadValueSafe(out LookRotation);
                reader.ReadValueSafe(out Velocity);
                reader.ReadValueSafe(out CrouchAmount);
                reader.ReadValueSafe(out SpeedMultiplier);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Tick);
                writer.WriteValueSafe(Position);
                writer.WriteValueSafe(LookRotation);
                writer.WriteValueSafe(Velocity);
                writer.WriteValueSafe(CrouchAmount);
                writer.WriteValueSafe(SpeedMultiplier);
            }
        }
        public static bool operator==(PlayerNetworkTransformState s1, PlayerNetworkTransformState s2)
        {
            if (s1 is null)
                return s2 is null;
            return s1.Equals(s2);
        }
        public static bool operator!=(PlayerNetworkTransformState s1, PlayerNetworkTransformState s2)
        {
            if (s1 is null)
                return !(s2 is null);
            return !s1.Equals(s2);
        }
        public override bool Equals(object obj)
        {
            PlayerNetworkTransformState otherState = (PlayerNetworkTransformState)obj;

            if (otherState is null)
                return false;

            if (Position == otherState.Position && LookRotation == otherState.LookRotation &&
                Velocity == otherState.Velocity && CrouchAmount == otherState.CrouchAmount && SpeedMultiplier == otherState.SpeedMultiplier)
                return true;

            return false;
        }
        public override int GetHashCode()
        {
            return (int)Position.x + (int)Position.y + (int)LookRotation.x + (int)LookRotation.y;
        }
    }
    public class PlayerNetworkTransformList : NetworkStateList<PlayerNetworkTransformState>, INetworkSerializable
    {
		public PlayerNetworkTransformList() : base() { }
        public PlayerNetworkTransformList(int ticksSaved) : base(ticksSaved) { }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int count);
                States = new();
                for (int i = 0; i < count; i++)
                {
                    reader.ReadValueSafe(out PlayerNetworkTransformState state);
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