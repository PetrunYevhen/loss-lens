namespace LossLens.Application.Exceptions;

/// <summary>The request names a line, station or shift that does not exist. The API maps it to 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);
