using Domain.Shared.Enums;
using Package.Infrastructure.Data.Contracts;

namespace Domain.Model;
public class SystemSetting : EntityBase
{
    public SystemSetting(string key, string? value)
    {
        Key = key;
        Value = value;
    }
    public string Key { get; set; }
    public string? Value { get; set; }
    public SystemSettings Flags { get; set; }
}
