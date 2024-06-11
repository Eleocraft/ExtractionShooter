using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerNetworkInputState : NetworkState, INetworkSerializable
    {
        public Vector2 MovementInput;
        public Vector2 LookDelta;
        public bool Run;
        public bool Crouch;
        public bool Jump;
        public PlayerNetworkInputState() {}
        public PlayerNetworkInputState(int tick)
        {
            Tick = tick;
        }
        public PlayerNetworkInputState(PlayerNetworkInputState oldState, int tick)
        {
            MovementInput = oldState.MovementInput;
            LookDelta = oldState.LookDelta;
            Run = oldState.Run;
            Jump = oldState.Jump;
            Crouch = oldState.Crouch;
            
            Tick = tick;
        }
        public PlayerNetworkInputState(int tick, Vector2 movementInput, Vector2 lookRotation, bool sprint, bool jump, bool crouch)
        {
            Tick = tick;
            MovementInput = movementInput;
            LookDelta = lookRotation;
            Run = sprint;
            Jump = jump;
            Crouch = crouch;
        }
        public override NetworkState GetStateWithTick(int tick) => new PlayerNetworkInputState(this, tick);
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Tick);
                reader.ReadValueSafe(out MovementInput);
                reader.ReadValueSafe(out LookDelta);
                reader.ReadValueSafe(out Run);
                reader.ReadValueSafe(out Jump);
                reader.ReadValueSafe(out Crouch);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Tick);
                writer.WriteValueSafe(MovementInput);
                writer.WriteValueSafe(LookDelta);
                writer.WriteValueSafe(Run);
                writer.WriteValueSafe(Jump);
                writer.WriteValueSafe(Crouch);
            }
        }
        public override bool Equals(object obj)
        {
            PlayerNetworkInputState otherState = (PlayerNetworkInputState)obj;
            
            if (otherState is null)
                return false;
            
            if (MovementInput == otherState.MovementInput && LookDelta == otherState.LookDelta &&
                Run == otherState.Run && Jump == otherState.Jump && Crouch == otherState.Crouch) return true;

            return false;
        }
        public override int GetHashCode()
        {
            return (int)MovementInput.x + (int)MovementInput.y + (int)LookDelta.x + (int)LookDelta.y;
        }
    }
    public class PlayerNetworkInputList : NetworkStateList<PlayerNetworkInputState>, INetworkSerializable
    {
        public PlayerNetworkInputList() : base() { }
        public PlayerNetworkInputList(int ticksSaved) : base(ticksSaved) {}
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int count);
                States = new();
                for (int i = 0; i < count; i++)
                {
                    reader.ReadValueSafe(out PlayerNetworkInputState state);
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