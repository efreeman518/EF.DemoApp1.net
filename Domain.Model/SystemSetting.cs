using Domain.Shared.Enums;
using Package.Infrastructure.Data.Contracts;

namespace Domain.Model;
public class SystemSetting(string key, string? value) : EntityBase
{
    public string Key { get; set; } = key;
    public string? Value { get; set; } = value;
    public SystemSettings Flags { get; set; }
}
