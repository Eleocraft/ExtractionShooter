using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public class NetworkInputState : INetworkSerializable
    {
        public int Tick;
        public Vector2 MovementInput;
        public Vector2 LookDelta;
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
            
            if (MovementInput == otherState.MovementInput && LookDelta == otherState.LookDelta &&
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
                    return new(tick);
                
                for (int i = 0; i < States.Count; i++)
                    if (States[i].Tick <= tick)
                        return States[i];

                return new(tick);
            }
            set
            {
                if (LastState?.Tick - _ticksSaved > tick) // Tick is to old
                    return;
                
                for (int i = 0; i < States.Count; i++)
                {
                    if (States[i].Tick == tick)
                    {
                        States[i] = value;
                        break;
                    }
                    else if (States[i].Tick < tick)
                    {
                        States.Insert(i, value);
                        break;
                    }
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
            if (inputState.Tick <= LastState?.Tick)
                return;
            
            if (LastState is not null && LastState == inputState)
                LastState.Tick = inputState.Tick;
            else
                States.Insert(0, inputState);
        }
        public void RemoveOutdated()
        {
            if (LastState is null)
                return;
            
            int startTick = LastState.Tick;
            bool hasFirstState = false;
            for (int i = 0; i < States.Count; i++)
            {
                if (States[i].Tick > startTick - _ticksSaved)
                    continue;

                if (!hasFirstState)
                    hasFirstState = true;
                else
                {
                    States.RemoveAt(i);
                    i--;
                }
            }
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