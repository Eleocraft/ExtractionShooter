using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class NetworkInputState : INetworkSerializable
    {
        public int Tick;
        public Vector2 MovementInput;
        public Vector2 LookRotation;
        public bool Sprint;
        public bool Jump;
        public NetworkInputState() {}
        public NetworkInputState(int tick)
        {
            Tick = tick;
        }
        public NetworkInputState(int tick, Vector2 movementInput, Vector2 lookRotation, bool sprint, bool jump)
        {
            Tick = tick;
            MovementInput = movementInput;
            LookRotation = lookRotation;
            Sprint = sprint;
            Jump = jump;
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Tick);
                reader.ReadValueSafe(out MovementInput);
                reader.ReadValueSafe(out LookRotation);
                reader.ReadValueSafe(out Sprint);
                reader.ReadValueSafe(out Jump);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Tick);
                writer.WriteValueSafe(MovementInput);
                writer.WriteValueSafe(LookRotation);
                writer.WriteValueSafe(Sprint);
                writer.WriteValueSafe(Jump);
            }
        }
    }
}