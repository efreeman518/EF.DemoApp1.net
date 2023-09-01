using Domain.Shared.Enums;

namespace Application.Contracts.Model;
public class SystemSettingDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public SystemSettings Flags { get; set; }
}
