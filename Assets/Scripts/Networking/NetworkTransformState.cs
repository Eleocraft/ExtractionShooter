using UnityEngine;
using Unity.Netcode;

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
    }
}