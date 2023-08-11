using Domain.Model;

namespace Test.Support;
public static class DbSupport
{
    /// <summary>
    /// InMemory provider does not understand RowVersion like Sql EF Provider
    /// </summary>
    /// <param name="item"></param>
    public static void ApplyRowVersion(this TodoItem item)
    {
        item.RowVersion = GetRandomByteArray(16);
    }

    public static byte[] GetRandomByteArray(int sizeInBytes)
    {
        Random rnd = new();
        byte[] b = new byte[sizeInBytes]; // convert kb to byte
        rnd.NextBytes(b);
        return b;
    }
}
