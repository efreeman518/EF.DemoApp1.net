﻿using Domain.Shared.Enums;
using Package.Infrastructure.Domain;

namespace Domain.Model;
public class SystemSetting(string key, string? value) : AuditableBase<string>
{
    public string Key { get; set; } = key;
    public string? Value { get; set; } = value;
    public SystemSettings Flags { get; set; }
}
