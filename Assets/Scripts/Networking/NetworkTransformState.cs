using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class NetworkTransformState : INetworkSerializable
    {
        public int Tick;
        public Vector3 Position;
        public Vector2 LookRotation;
        public float CurrentHorizontalSpeed;
        public float VerticalVelocity;

        public NetworkTransformState() {}
        public NetworkTransformState(int tick)
        {
            Tick = tick;
        }
        public NetworkTransformState(int tick, Vector3 position, Vector2 lookRotation, float currentHorizontalSpeed, float verticalVelocity)
        {
            Tick = tick;
            Position = position;
            LookRotation = lookRotation;
            CurrentHorizontalSpeed = currentHorizontalSpeed;
            VerticalVelocity = verticalVelocity;
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Tick);
                reader.ReadValueSafe(out Position);
                reader.ReadValueSafe(out LookRotation);
                reader.ReadValueSafe(out CurrentHorizontalSpeed);
                reader.ReadValueSafe(out VerticalVelocity);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Tick);
                writer.WriteValueSafe(Position);
                writer.WriteValueSafe(LookRotation);
                writer.WriteValueSafe(CurrentHorizontalSpeed);
                writer.WriteValueSafe(VerticalVelocity);
            }
        }
    }
}