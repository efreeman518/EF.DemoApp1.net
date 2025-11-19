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
        if (Unsafe.IsNullRef(ref val))
        {
            return false;
        }
        val = newValue;
        return true;
    }

    /// <summary>
    /// Retrieves the value associated with the first key in the specified sequence that exists in the dictionary.
    /// </summary>
    /// <remarks>This method iterates through the provided keys in order and returns the value of the first
    /// key that exists in the dictionary. If none of the keys are found, the method returns the default value for
    /// <typeparamref name="TVal"/>.</remarks>
    /// <typeparam name="TKey">The type of the keys in the dictionary. Must be a non-nullable type.</typeparam>
    /// <typeparam name="TVal">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to search for the specified keys.</param>
    /// <param name="keys">An array of keys to search for in the dictionary, in order of priority.</param>
    /// <returns>The value associated with the first key found in the dictionary; otherwise, the default value for <typeparamref
    /// name="TVal"/>.</returns>
    public static TVal? GetFirstKeyValue<TKey, TVal>(this Dictionary<TKey, TVal> dictionary, params TKey[] keys) where TKey : notnull
    {
        foreach (var key in keys)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return default;
    }
}
