using Application.Contracts.Model;
using Domain.Model;

namespace Application.Contracts.Mappers;

public static class SystemSettingMapper
{
    public static SystemSettingDto Projector(SystemSetting item)
    {
        return new SystemSettingDto(item.Id, item.Key, item.Value, item.Flags);
    }
}
