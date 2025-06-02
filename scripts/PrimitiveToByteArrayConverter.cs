
using System.Collections.Generic;
using System;
using System.Text;

public static class PrimitiveToByteArrayConverter
{
    // ---- TO BYTES ----
    public static byte[] ToBytes(bool value) => BitConverter.GetBytes(value);
    public static byte[] ToBytes(char value) => BitConverter.GetBytes(value);
    public static byte[] ToBytes(byte value) => new[] { value };
    public static byte[] ToBytes(sbyte value) => new[] { (byte)value };
    public static byte[] ToBytes(short value) => BitConverter.GetBytes(value);
    public static byte[] ToBytes(ushort value) => BitConverter.GetBytes(value);
    public static byte[] ToBytes(int value) => BitConverter.GetBytes(value);
    public static byte[] ToBytes(uint value) => BitConverter.GetBytes(value);
    public static byte[] ToBytes(long value) => BitConverter.GetBytes(value);
    public static byte[] ToBytes(ulong value) => BitConverter.GetBytes(value);
    public static byte[] ToBytes(float value) => BitConverter.GetBytes(value);
    public static byte[] ToBytes(double value) => BitConverter.GetBytes(value);
    public static byte[] ToBytes(string value) => Encoding.UTF8.GetBytes(value);

    public static byte[] ToBytes(bool[] values) => Flatten(values, ToBytes);
    public static byte[] ToBytes(char[] values) => Flatten(values, ToBytes);
    public static byte[] ToBytes(byte[] values) => values;
    public static byte[] ToBytes(sbyte[] values) => Flatten(values, ToBytes);
    public static byte[] ToBytes(short[] values) => Flatten(values, ToBytes);
    public static byte[] ToBytes(ushort[] values) => Flatten(values, ToBytes);
    public static byte[] ToBytes(int[] values) => Flatten(values, ToBytes);
    public static byte[] ToBytes(uint[] values) => Flatten(values, ToBytes);
    public static byte[] ToBytes(long[] values) => Flatten(values, ToBytes);
    public static byte[] ToBytes(ulong[] values) => Flatten(values, ToBytes);
    public static byte[] ToBytes(float[] values) => Flatten(values, ToBytes);
    public static byte[] ToBytes(double[] values) => Flatten(values, ToBytes);
    public static byte[] ToBytes(string[] values) => Flatten(values, Encoding.UTF8.GetBytes, new byte[] { 0 });

    public static byte[] ToBytes<T>(T value)
    {
        switch (value)
        {
            case bool v: return ToBytes(v);
            case char v: return ToBytes(v);
            case byte v: return ToBytes(v);
            case sbyte v: return ToBytes(v);
            case short v: return ToBytes(v);
            case ushort v: return ToBytes(v);
            case int v: return ToBytes(v);
            case uint v: return ToBytes(v);
            case long v: return ToBytes(v);
            case ulong v: return ToBytes(v);
            case float v: return ToBytes(v);
            case double v: return ToBytes(v);
            case string v: return ToBytes(v);

            case bool[] arr: return ToBytes(arr);
            case char[] arr: return ToBytes(arr);
            case byte[] arr: return ToBytes(arr);
            case sbyte[] arr: return ToBytes(arr);
            case short[] arr: return ToBytes(arr);
            case ushort[] arr: return ToBytes(arr);
            case int[] arr: return ToBytes(arr);
            case uint[] arr: return ToBytes(arr);
            case long[] arr: return ToBytes(arr);
            case ulong[] arr: return ToBytes(arr);
            case float[] arr: return ToBytes(arr);
            case double[] arr: return ToBytes(arr);
            case string[] arr: return ToBytes(arr);

            default:
                throw new NotSupportedException($"Type {typeof(T)} is not supported.");
        }
    }

    // ---- FROM BYTES ----
    public static bool FromBytesBool(byte[] bytes, int offset = 0) => BitConverter.ToBoolean(bytes, offset);
    public static char FromBytesChar(byte[] bytes, int offset = 0) => BitConverter.ToChar(bytes, offset);
    public static byte FromBytesByte(byte[] bytes, int offset = 0) => bytes[offset];
    public static sbyte FromBytesSByte(byte[] bytes, int offset = 0) => (sbyte)bytes[offset];
    public static short FromBytesShort(byte[] bytes, int offset = 0) => BitConverter.ToInt16(bytes, offset);
    public static ushort FromBytesUShort(byte[] bytes, int offset = 0) => BitConverter.ToUInt16(bytes, offset);
    public static int FromBytesInt(byte[] bytes, int offset = 0) => BitConverter.ToInt32(bytes, offset);
    public static uint FromBytesUInt(byte[] bytes, int offset = 0) => BitConverter.ToUInt32(bytes, offset);
    public static long FromBytesLong(byte[] bytes, int offset = 0) => BitConverter.ToInt64(bytes, offset);
    public static ulong FromBytesULong(byte[] bytes, int offset = 0) => BitConverter.ToUInt64(bytes, offset);
    public static float FromBytesFloat(byte[] bytes, int offset = 0) => BitConverter.ToSingle(bytes, offset);
    public static double FromBytesDouble(byte[] bytes, int offset = 0) => BitConverter.ToDouble(bytes, offset);
    public static string FromBytesString(byte[] bytes) => Encoding.UTF8.GetString(bytes);


    public static T[] FromBytes<T>(byte[] bytes) where T : struct
    {
        Type t = typeof(T);

        if (t == typeof(bool))
            return (T[])(object)FromBytesArray(bytes, sizeof(bool), BitConverter.ToBoolean);
        if (t == typeof(char))
            return (T[])(object)FromBytesArray(bytes, sizeof(char), BitConverter.ToChar);
        if (t == typeof(byte))
            return (T[])(object)bytes;
        if (t == typeof(short))
            return (T[])(object)FromBytesArray(bytes, sizeof(short), BitConverter.ToInt16);
        if (t == typeof(ushort))
            return (T[])(object)FromBytesArray(bytes, sizeof(ushort), BitConverter.ToUInt16);
        if (t == typeof(int))
            return (T[])(object)FromBytesArray(bytes, sizeof(int), BitConverter.ToInt32);
        if (t == typeof(uint))
            return (T[])(object)FromBytesArray(bytes, sizeof(uint), BitConverter.ToUInt32);
        if (t == typeof(long))
            return (T[])(object)FromBytesArray(bytes, sizeof(long), BitConverter.ToInt64);
        if (t == typeof(ulong))
            return (T[])(object)FromBytesArray(bytes, sizeof(ulong), BitConverter.ToUInt64);
        if (t == typeof(float))
            return (T[])(object)FromBytesArray(bytes, sizeof(float), BitConverter.ToSingle);
        if (t == typeof(double))
            return (T[])(object)FromBytesArray(bytes, sizeof(double), BitConverter.ToDouble);

        throw new NotSupportedException($"Type {typeof(T)} is not supported.");
    }

    private static T[] FromBytesArray<T>(byte[] bytes, int elementSize, Func<byte[], int, T> converter)
    {
        int count = bytes.Length / elementSize;
        T[] result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = converter(bytes, i * elementSize);
        return result;
    }

    // Helper for string[]
    public static string[] FromBytesStringArray(byte[] bytes)
    {
        var segments = new List<string>();
        int start = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 0) // null-byte separator
            {
                int len = i - start;
                segments.Add(Encoding.UTF8.GetString(bytes, start, len));
                start = i + 1;
            }
        }

        if (start < bytes.Length)
        {
            segments.Add(Encoding.UTF8.GetString(bytes, start, bytes.Length - start));
        }

        return segments.ToArray();
    }

    // ---- HELPER ----
    private static byte[] Flatten<T>(T[] values, Func<T, byte[]> converter, byte[]? separator = null)
    {
        if (values == null || values.Length == 0)
            return Array.Empty<byte>();

        var totalSize = 0;
        var byteArrays = new byte[values.Length][];

        for (int i = 0; i < values.Length; i++)
        {
            byteArrays[i] = converter(values[i]);
            totalSize += byteArrays[i].Length;
        }

        if (separator != null)
            totalSize += separator.Length * (values.Length - 1);

        var result = new byte[totalSize];
        int offset = 0;

        for (int i = 0; i < byteArrays.Length; i++)
        {
            Buffer.BlockCopy(byteArrays[i], 0, result, offset, byteArrays[i].Length);
            offset += byteArrays[i].Length;

            if (separator != null && i < byteArrays.Length - 1)
            {
                Buffer.BlockCopy(separator, 0, result, offset, separator.Length);
                offset += separator.Length;
            }
        }

        return result;
    }
}
