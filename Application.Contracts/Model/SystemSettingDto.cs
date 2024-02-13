using Domain.Shared.Enums;

namespace Application.Contracts.Model;
public record SystemSettingDto(Guid Id, string Key, string? Value, SystemSettings Flags);

