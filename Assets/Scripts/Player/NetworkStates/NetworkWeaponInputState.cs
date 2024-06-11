using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class NetworkWeaponInputState : NetworkState, INetworkSerializable
    {
        public bool PrimaryAction;
        public bool SecondaryAction;
        public bool ReloadAction;
        public int ServerTickOnCreation;
        public int TickDiff;
        public NetworkWeaponInputState() {}
        public NetworkWeaponInputState(NetworkWeaponInputState oldState, int tick)
        {
            PrimaryAction = oldState.PrimaryAction;
            SecondaryAction = oldState.SecondaryAction;
            ReloadAction = oldState.ReloadAction;
            ServerTickOnCreation = oldState.ServerTickOnCreation;

            Tick = tick;
        }
        public NetworkWeaponInputState(bool primaryAction, bool secondaryAction, bool reloadAction, int serverTick, int tick)
        {
            PrimaryAction = primaryAction;
            SecondaryAction = secondaryAction;
            ReloadAction = reloadAction;
            ServerTickOnCreation = serverTick;
            Tick = tick;
        }
        public int SetTickDiff() => NetworkManager.Singleton.LocalTime.Tick - ServerTickOnCreation;
        public override NetworkState GetStateWithTick(int tick) => new NetworkWeaponInputState(this, tick);
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Tick);
                reader.ReadValueSafe(out PrimaryAction);
                reader.ReadValueSafe(out SecondaryAction);
                reader.ReadValueSafe(out ReloadAction);
                reader.ReadValueSafe(out ServerTickOnCreation);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Tick);
                writer.WriteValueSafe(PrimaryAction);
                writer.WriteValueSafe(SecondaryAction);
                writer.WriteValueSafe(ReloadAction);
                writer.WriteValueSafe(ServerTickOnCreation);
            }
        }
        public override bool Equals(object obj)
        {
            NetworkWeaponInputState otherState = (NetworkWeaponInputState)obj;

            if (otherState is null)
                return false;

            if (PrimaryAction == otherState.PrimaryAction && SecondaryAction == otherState.SecondaryAction &&
                ReloadAction == otherState.ReloadAction && ServerTickOnCreation == otherState.ServerTickOnCreation)
                return true;
            
            return false;
        }

        public override int GetHashCode()
        {
            return PrimaryAction ? 1 : 0;
        }
    }
    public class NetworkWeaponInputStateList : NetworkStateList<NetworkWeaponInputState>, INetworkSerializable
    {
        public NetworkWeaponInputStateList() : base() { }
        public NetworkWeaponInputStateList(int ticksSaved) : base(ticksSaved) { }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int count);
                States = new();
                for (int i = 0; i < count; i++)
                {
                    reader.ReadValueSafe(out NetworkWeaponInputState state);
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
