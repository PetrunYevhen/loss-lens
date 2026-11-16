namespace LossLens.Application.Exceptions;

/// <summary>Sign-in failed. The API maps it to 401.</summary>
public sealed class UnauthorizedException(string message) : Exception(message);
