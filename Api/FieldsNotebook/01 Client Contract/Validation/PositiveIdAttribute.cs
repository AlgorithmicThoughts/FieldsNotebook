using System.ComponentModel.DataAnnotations;

namespace DataContract.Validation;

/// <summary>A database identity: one or greater. Fails model validation with a 400 before any Manager runs.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class PositiveIdAttribute : RangeAttribute
{
    public PositiveIdAttribute() : base(1, int.MaxValue)
    {
        ErrorMessage = "{0} must be greater than zero.";
    }
}
