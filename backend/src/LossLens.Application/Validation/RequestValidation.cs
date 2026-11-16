using LossLens.Application.Exceptions;

namespace LossLens.Application.Validation;

internal static class RequestValidation
{
    /// <summary>Longest date range a single request may cover, to keep queries bounded.</summary>
    public const int MaxRangeDays = 92;

    /// <exception cref="ValidationException">The range is reversed or longer than <see cref="MaxRangeDays"/>.</exception>
    public static void EnsureDateRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new ValidationException("'from' must not be later than 'to'.");

        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
            throw new ValidationException($"The date range must not exceed {MaxRangeDays} days.");
    }
}
