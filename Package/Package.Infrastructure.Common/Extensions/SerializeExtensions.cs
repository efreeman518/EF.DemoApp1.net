using Package.Infrastructure.Common.Attributes;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Package.Infrastructure.Common.Extensions;
public static class SerializeExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="toSerialize"></param>
    /// <param name="options"></param>
    /// <param name="applyMasks"></param>
    /// <returns></returns>
    public static string? SerializeToJson<T>(this T toSerialize, JsonSerializerOptions? options = null, bool applyMasks = true)
    {
        if (toSerialize == null) return null;
        if (!applyMasks) return JsonSerializer.Serialize(toSerialize, options);

        MaskAttribute maskAttr;

        //check class for mask
        var maskCheck = Attribute.GetCustomAttribute(toSerialize.GetType(), typeof(MaskAttribute));
        if (maskCheck != null)
        {
            maskAttr = (MaskAttribute)maskCheck;
            if (maskAttr.MatchPattern == null)
                return maskAttr.Mask;

            return Regex.Replace(JsonSerializer.Serialize(toSerialize, options), maskAttr.MatchPattern, maskAttr.Mask);
        }

        //check props for mask
        PropertyInfo[] propInfo = toSerialize.GetType().GetProperties();
        var maskedProps = propInfo.Where(pi => Attribute.GetCustomAttribute(pi, typeof(MaskAttribute)) != null).Select(pi => pi);
        if (maskedProps.Any())
        {
            string propMask;
            string? propVal;
            var newT = DeserializeJson<T>(JsonSerializer.Serialize(toSerialize, options)); //Clone
            foreach (var prop in maskedProps)
            {
                propVal = prop.GetValue(newT)?.ToString();
                if (propVal != null)
                {
                    maskAttr = (MaskAttribute)Attribute.GetCustomAttribute(prop, typeof(MaskAttribute))!;
                    propMask = maskAttr.Mask;
                    if (maskAttr.MatchPattern == null)
                        prop.SetValue(newT, propMask);
                    else
                        prop.SetValue(newT, Regex.Replace(propVal, maskAttr.MatchPattern, propMask));
                }
            }
            return JsonSerializer.Serialize(newT!, options);
        }

        //there were no mask attributes
        return JsonSerializer.Serialize(toSerialize, options);
    }

    /// <summary>
    /// Deserializes json string in to T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="toDeserialize"></param>
    /// <returns></returns>
    public static T? DeserializeJson<T>(this string toDeserialize, JsonSerializerOptions? options = null, bool throwOnNull = false)
    {
        T? val = JsonSerializer.Deserialize<T>(toDeserialize, options);
        if (val == null && throwOnNull) throw new InvalidOperationException($"JsonSerializer.Deserialize<T> returned null.");
        return val;
    }
}
