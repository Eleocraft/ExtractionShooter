using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class NetworkInputState : NetworkState, INetworkSerializable
    {
        public Vector2 MovementInput;
        public Vector2 LookDelta;
        public bool Sprint;
        public bool Jump;
        public NetworkInputState() {}
        public NetworkInputState(int tick)
        {
            Tick = tick;
        }
        public NetworkInputState(NetworkInputState oldState, int tick)
        {
            MovementInput = oldState.MovementInput;
            LookDelta = oldState.LookDelta;
            Sprint = oldState.Sprint;
            Jump = oldState.Jump;
            
            Tick = tick;
        }
        public NetworkInputState(int tick, Vector2 movementInput, Vector2 lookRotation, bool sprint, bool jump)
        {
            Tick = tick;
            MovementInput = movementInput;
            LookDelta = lookRotation;
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
                reader.ReadValueSafe(out LookDelta);
                reader.ReadValueSafe(out Sprint);
                reader.ReadValueSafe(out Jump);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Tick);
                writer.WriteValueSafe(MovementInput);
                writer.WriteValueSafe(LookDelta);
                writer.WriteValueSafe(Sprint);
                writer.WriteValueSafe(Jump);
            }
        }
        public override bool Equals(object obj)
        {
            NetworkInputState otherState = (NetworkInputState)obj;
            
            if (otherState is null)
                return false;
            
            if (MovementInput == otherState.MovementInput && LookDelta == otherState.LookDelta &&
                Sprint == otherState.Sprint && Jump == otherState.Jump) return true;

            return false;
        }
        public override int GetHashCode()
        {
            return (int)MovementInput.x + (int)MovementInput.y + (int)LookDelta.x + (int)LookDelta.y;
        }
    }
    public class NetworkInputStateList : NetworkStateList<NetworkInputState>, INetworkSerializable
    {
        public NetworkInputStateList() { }
        public NetworkInputStateList(int ticksSaved)
        {
            _ticksSaved = ticksSaved;
        }
        public NetworkInputStateList GetListForTicks(int ticks)
        {
            if (LastState is null)
                return null;
            
            NetworkInputStateList newList = new(ticks);
            int startTick = LastState.Tick;
            for (int i = 0; i < States.Count; i++)
            {
                if (States[i].Tick < startTick - ticks)
                    break;
                
                newList.States.Add(States[i]);
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
                    reader.ReadValueSafe(out NetworkInputState state);
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