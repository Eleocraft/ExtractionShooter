using UnityEngine;
using Unity.Netcode;

public class NetworkTransformState : INetworkSerializable
{
    public int Tick;
    public Vector3 Position;
    public Quaternion Rotation;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            FastBufferReader reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out Tick);
            reader.ReadValueSafe(out Position);
            reader.ReadValueSafe(out Rotation);
        }
        else
        {
            FastBufferWriter writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(Tick);
            writer.WriteValueSafe(Position);
            writer.WriteValueSafe(Rotation);
        }
    }
}
