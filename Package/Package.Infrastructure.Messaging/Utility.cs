using System.Text;

namespace Package.Infrastructure.Messaging;
internal static class Utility
{
    public static string StringToBinary(this string data)
    {
        StringBuilder sb = new();

        foreach (char c in data)
        {
            sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
        }
        return sb.ToString();
    }

    public static string BinaryToString(this string data, Encoding encoding)
    {
        List<Byte> byteList = [];

        for (int i = 0; i < data.Length; i += 8)
        {
            byteList.Add(Convert.ToByte(data.Substring(i, 8), 2));
        }
        return encoding.GetString([.. byteList]);
    }
}
