namespace Package.Infrastructure.Common;

public static class TypeUtility
{
    /// <summary>
    /// Determine if <typeparamref name="T"/> (or its base type in case of List<typeparamref name="T"/>) inherits from TBase 
    /// </summary>
    /// <typeparam name="T">The type to check</typeparam>
    /// <typeparam name="TBase">The type we are looking for</typeparam>
    /// <returns></returns>
    public static bool IsTypeAssignable<T, TBase>()
    {
        Type typeToCheck = typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>) ? typeof(T).GetGenericArguments().Single() : typeof(T);
        return typeof(TBase).IsAssignableFrom(typeToCheck);
    }
}
