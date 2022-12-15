using System.Collections.Generic;

namespace Package.Infrastructure.Utility;
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Messages { get; set; }

    public ValidationResult(bool valid, List<string>? messages = null)
    {
        IsValid = valid;
        Messages = messages ?? new List<string>();
    }
    public static ValidationResult True(List<string>? messages = null) => new(true, messages);
    public static ValidationResult False(List<string>? messages = null) => new(false, messages);
    public static ValidationResult Assign(bool valid, List<string>? messages = null) => new(valid, messages);

    //simplify boolean operations
    public static implicit operator bool(ValidationResult value) => value.IsValid;
    public static explicit operator ValidationResult(bool valid) => Assign(valid);

    public override int GetHashCode()
    {
        return IsValid.GetHashCode();
    }

    //Equality ignores messages

    public override bool Equals(object? obj)
    {
        return Equals(obj as ValidationResult);
    }

    public bool Equals(ValidationResult? vr)
    {
        if (ReferenceEquals(this, vr)) return true;
        return IsValid == vr?.IsValid;
    }

    public static bool operator ==(ValidationResult value1, ValidationResult value2)
    {
        return value1.IsValid == value2.IsValid;
    }
    public static bool operator !=(ValidationResult value1, ValidationResult value2)
    {
        return value1.IsValid != value2.IsValid;
    }

    
}
