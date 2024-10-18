using K4os.Compression.LZ4.Streams;
using System.IO.Compression;
using System.Text;

namespace Package.Infrastructure.Common.Extensions;

public enum CompressionAlogrithm
{
    GZip = 0,
    LZ4 = 1
    //add more as needed
}

public static class CompressionExtensions
{
    public static byte[]? CompressToByteArray<T>(this T toCompress, CompressionAlogrithm algo) where T : class?
    {
        if (toCompress == null) return null;

        string? ser = toCompress.SerializeToJson<T>(applyMasks: false);
        if (ser == null) return null;

        return ser.CompressToByteArray(algo);
    }

    public static byte[] CompressToByteArray(this string toCompress, CompressionAlogrithm algo)
    {
        byte[] byteArray = Encoding.UTF8.GetBytes(toCompress);
        using var memoryStream = new MemoryStream();
        switch (algo)
        {
            case CompressionAlogrithm.GZip:
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gzipStream.Write(byteArray, 0, byteArray.Length);
                }
                break;
            case CompressionAlogrithm.LZ4:
                //https://github.com/MiloszKrajewski/K4os.Compression.LZ4
                using (var lz4Stream = LZ4Stream.Encode(memoryStream, K4os.Compression.LZ4.LZ4Level.L00_FAST))
                {
                    lz4Stream.Write(byteArray, 0, byteArray.Length);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(algo), algo, null);
        }

        return memoryStream.ToArray();
    }

    public static T? DecompressFromByteArray<T>(this byte[]? toDecompress, CompressionAlogrithm algo) where T : class?
    {
        if (toDecompress == null) return null;
        string? ser;
        using var memoryStream = new MemoryStream(toDecompress);
        switch (algo)
        {
            case CompressionAlogrithm.GZip:
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(gzipStream))
                    ser = reader.ReadToEnd();
                break;
            case CompressionAlogrithm.LZ4:
                using (var lz4Stream = LZ4Stream.Decode(memoryStream))
                using (var reader = new StreamReader(lz4Stream))
                    ser = reader.ReadToEnd();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(algo), algo, null);
        }
        return ser.DeserializeJson<T>();
    }
}
