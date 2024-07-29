using Application.Contracts.Model;
using Domain.Model;
using System.Linq.Expressions;

namespace Application.Contracts.Mappers;

public static class SystemSettingMapper
{
    public static readonly Expression<Func<SystemSetting, SystemSettingDto>> Projector = static item => new(item.Id, item.Key, item.Value, item.Flags);
}
