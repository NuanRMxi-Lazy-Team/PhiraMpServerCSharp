namespace PhiraMp.Core;

/// <summary>
/// Interface for types that can be serialized/deserialized
/// </summary>
public interface IBinaryData
{
    void WriteBinary(BinaryWriter writer);
}
