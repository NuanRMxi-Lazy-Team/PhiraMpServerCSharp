namespace PhiraMp.Core;

public readonly struct Half
{
    private readonly ushort _value;

    public Half(ushort bits) => _value = bits;

    public static Half FromFloat(float value) => new Half(BitConverter.HalfToUInt16Bits((System.Half)value));
    public float ToFloat() => (float)BitConverter.UInt16BitsToHalf(_value);
    public ushort ToBits() => _value;
}
