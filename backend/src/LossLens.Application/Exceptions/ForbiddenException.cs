namespace LossLens.Application.Exceptions;

/// <summary>The caller asked for data outside their role scope. The API maps it to 403.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
