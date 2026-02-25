using System.Buffers.Binary;
using System.Text;

namespace PhiraMp.Core;

/// <summary>
/// Binary writer compatible with Rust implementation
/// Uses little-endian encoding and ULEB128 for variable-length integers
/// </summary>
public class BinaryWriter
{
    private readonly List<byte> _buffer;

    public BinaryWriter()
    {
        _buffer = new List<byte>();
    }

    public byte[] ToArray() => _buffer.ToArray();

    public void WriteByte(byte value)
    {
        _buffer.Add(value);
    }

    public void WriteSByte(sbyte value)
    {
        _buffer.Add((byte)value);
    }

    public void WriteBool(bool value)
    {
        _buffer.Add(value ? (byte)1 : (byte)0);
    }

    public void WriteUInt16(ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        _buffer.AddRange(bytes.ToArray());
    }

    public void WriteUInt32(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        _buffer.AddRange(bytes.ToArray());
    }

    public void WriteUInt64(ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        _buffer.AddRange(bytes.ToArray());
    }

    public void WriteInt32(int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        _buffer.AddRange(bytes.ToArray());
    }

    public void WriteInt64(long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        _buffer.AddRange(bytes.ToArray());
    }

    public void WriteSingle(float value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(bytes, value);
        _buffer.AddRange(bytes.ToArray());
    }

    /// <summary>
    /// Writes ULEB128 (unsigned LEB128) encoded integer
    /// </summary>
    public void WriteULEB(ulong value)
    {
        do
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
                b |= 0x80;
            _buffer.Add(b);
        } while (value != 0);
    }

    public void WriteString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteULEB((ulong)bytes.Length);
        _buffer.AddRange(bytes);
    }

    public void WriteArray<T>(IReadOnlyList<T> array, Action<BinaryWriter, T> writeFunc)
    {
        WriteULEB((ulong)array.Count);
        foreach (var item in array)
        {
            writeFunc(this, item);
        }
    }
}
