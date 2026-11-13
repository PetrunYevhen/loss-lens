using System.Runtime.CompilerServices;

namespace LossLens.Core.Common;

/// <summary>Shared argument checks for model invariants.</summary>
internal static class Guard
{
    public static Guid NotEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value cannot be an empty GUID.", paramName);

        return value;
    }

    public static int HourOfDay(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 23, paramName);

        return value;
    }
}
