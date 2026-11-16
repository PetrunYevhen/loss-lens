namespace LossLens.Application.Exceptions;

/// <summary>A request parameter is invalid. The API maps it to 400.</summary>
public sealed class ValidationException(string message) : Exception(message);
