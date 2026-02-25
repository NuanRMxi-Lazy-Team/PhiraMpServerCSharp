using System.Buffers.Binary;
using System.Text;

namespace PhiraMp.Core;

/// <summary>
/// Binary reader compatible with Rust implementation
/// Uses little-endian encoding and ULEB128 for variable-length integers
/// </summary>
public class BinaryReader
{
    private readonly byte[] _data;
    private int _position;

    public BinaryReader(byte[] data)
    {
        _data = data;
        _position = 0;
    }

    public byte ReadByte()
    {
        if (_position >= _data.Length)
            throw new EndOfStreamException("Unexpected EOF");
        return _data[_position++];
    }

    public sbyte ReadSByte() => (sbyte)ReadByte();

    public bool ReadBool() => ReadByte() == 1;

    public ushort ReadUInt16()
    {
        if (_position + 2 > _data.Length)
            throw new EndOfStreamException("Unexpected EOF");
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(_position, 2));
        _position += 2;
        return value;
    }

    public uint ReadUInt32()
    {
        if (_position + 4 > _data.Length)
            throw new EndOfStreamException("Unexpected EOF");
        var value = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(_position, 4));
        _position += 4;
        return value;
    }

    public ulong ReadUInt64()
    {
        if (_position + 8 > _data.Length)
            throw new EndOfStreamException("Unexpected EOF");
        var value = BinaryPrimitives.ReadUInt64LittleEndian(_data.AsSpan(_position, 8));
        _position += 8;
        return value;
    }

    public int ReadInt32()
    {
        if (_position + 4 > _data.Length)
            throw new EndOfStreamException("Unexpected EOF");
        var value = BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(_position, 4));
        _position += 4;
        return value;
    }

    public long ReadInt64()
    {
        if (_position + 8 > _data.Length)
            throw new EndOfStreamException("Unexpected EOF");
        var value = BinaryPrimitives.ReadInt64LittleEndian(_data.AsSpan(_position, 8));
        _position += 8;
        return value;
    }

    public float ReadSingle()
    {
        if (_position + 4 > _data.Length)
            throw new EndOfStreamException("Unexpected EOF");
        var value = BinaryPrimitives.ReadSingleLittleEndian(_data.AsSpan(_position, 4));
        _position += 4;
        return value;
    }

    public byte[] Take(int count)
    {
        if (_position + count > _data.Length)
            throw new EndOfStreamException("Unexpected EOF");
        var result = new byte[count];
        Array.Copy(_data, _position, result, 0, count);
        _position += count;
        return result;
    }

    /// <summary>
    /// Reads ULEB128 (unsigned LEB128) encoded integer
    /// </summary>
    public ulong ReadULEB()
    {
        ulong result = 0;
        int shift = 0;

        while (true)
        {
            byte b = ReadByte();
            result |= ((ulong)(b & 0x7F)) << shift;
            if ((b & 0x80) == 0)
                break;
            shift += 7;
        }

        return result;
    }

    public string ReadString()
    {
        var length = (int)ReadULEB();
        var bytes = Take(length);
        return Encoding.UTF8.GetString(bytes);
    }

    public List<T> ReadArray<T>(Func<BinaryReader, T> readFunc)
    {
        var count = (int)ReadULEB();
        var result = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            result.Add(readFunc(this));
        }
        return result;
    }
}
