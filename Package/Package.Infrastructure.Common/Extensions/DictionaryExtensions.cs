using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Package.Infrastructure.Common.Extensions;
public static class DictionaryExtensions
{
    //chapsas - https://www.youtube.com/watch?v=8dI_nsmcW-4

    public static TValue? GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue? value)
        where TKey : notnull
    {
        ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out var exists);
        if ((exists))
        {
            return val;
        }
        val = value;
        return value;
    }

    public static bool TryUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue newValue)
        where TKey : notnull
    {
        ref var val = ref CollectionsMarshal.GetValueRefOrNullRef(dictionary, key);
        if(Unsafe.IsNullRef(ref val))
        {
            return false;
        }
        val = newValue;
        return true;
    }
}
