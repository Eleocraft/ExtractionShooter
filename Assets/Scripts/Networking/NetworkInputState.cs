using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

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
        public static bool operator==(NetworkInputState s1, NetworkInputState s2)
        {
            if (s1 is null)
                return s2 is null;
            return s1.Equals(s2);
        }
        public static bool operator!=(NetworkInputState s1, NetworkInputState s2)
        {
            if (s1 is null)
                return !(s2 is null);
            return !s1.Equals(s2);
        }
        public override bool Equals(object obj)
        {
            NetworkInputState otherState = (NetworkInputState)obj;
            
            if (otherState is null)
                return false;
            
            if (MovementInput == otherState.MovementInput && LookRotation == otherState.LookRotation &&
                Sprint == otherState.Sprint && Jump == otherState.Jump) return true;

            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    public class NetworkInputStateList : INetworkSerializable
    {
		private int _ticksSaved;
        private List<NetworkInputState> States = new();
        public NetworkInputState LastState => States.Count > 0 ? States[0] : null;
        public NetworkInputState this[int tick]
        {
            get
            {
                if (LastState?.Tick - _ticksSaved > tick) // Tick is to old
                    return null;
                
                for (int i = States.Count - 1; i >= 0; i--)
                    if (States[i].Tick <= tick)
                        return States[i];

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
        public NetworkInputStateList() { }
        public NetworkInputStateList(int ticksSaved)
        {
            _ticksSaved = ticksSaved;
        }
        public void Add(NetworkInputState inputState)
        {
            if (inputState.Tick <= LastState.Tick)
                return;
            
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
        public NetworkInputStateList GetListForTicks(int ticks)
        {
            if (States.Count == 0)
            {
                Debug.Log("test");
                return null;
            }
            
            NetworkInputStateList newList = new(ticks);
            int startTick = LastState.Tick;
            for (int i = States.Count - 1; i >= 0; i--)
            {
                if (States[i].Tick < startTick - ticks)
                    break;
                
                newList.Add(States[i]);
            }
            Debug.Log("in" + newList.States.Count);
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