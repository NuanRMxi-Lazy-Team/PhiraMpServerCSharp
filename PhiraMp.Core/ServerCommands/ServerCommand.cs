namespace PhiraMp.Core;

public abstract class ServerCommand : IBinaryData
{
    public abstract byte TypeTag { get; }
    public abstract void WriteBinary(BinaryWriter writer);
}
