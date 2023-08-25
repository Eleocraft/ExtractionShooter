using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public class NetworkWeaponInputState : NetworkState, INetworkSerializable
    {
        public bool PrimaryAction;
        public bool SecondaryAction;
        public int ServerTickOnCreation;
        public NetworkWeaponInputState() {}
        public NetworkWeaponInputState(NetworkWeaponInputState oldState, int tick)
        {
            PrimaryAction = oldState.PrimaryAction;
            SecondaryAction = oldState.SecondaryAction;
            ServerTickOnCreation = oldState.ServerTickOnCreation;

            Tick = tick;
        }
        public NetworkWeaponInputState(bool primaryAction, bool secondaryAction, int serverTick, int tick)
        {
            PrimaryAction = primaryAction;
            SecondaryAction = secondaryAction;
            ServerTickOnCreation = serverTick;
            Tick = tick;
        }
        public override NetworkState GetStateWithTick(int tick) => new NetworkWeaponInputState(this, tick);
        public int TickDiff => NetworkManager.Singleton.ServerTime.Tick - ServerTickOnCreation;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Tick);
                reader.ReadValueSafe(out PrimaryAction);
                reader.ReadValueSafe(out SecondaryAction);
                reader.ReadValueSafe(out ServerTickOnCreation);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Tick);
                writer.WriteValueSafe(PrimaryAction);
                writer.WriteValueSafe(SecondaryAction);
                writer.WriteValueSafe(ServerTickOnCreation);
            }
        }
        public override bool Equals(object obj)
        {
            NetworkWeaponInputState otherState = (NetworkWeaponInputState)obj;

            if (otherState is null)
                return false;

            if (PrimaryAction == otherState.PrimaryAction && SecondaryAction == otherState.SecondaryAction)
                return true;
            
            return false;
        }

        public override int GetHashCode()
        {
            return PrimaryAction ? 1 : 0;
        }
    }
}
