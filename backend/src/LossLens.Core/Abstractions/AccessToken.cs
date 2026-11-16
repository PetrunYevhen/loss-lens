namespace LossLens.Core.Abstractions;

/// <param name="Value">The signed token the client sends in the Authorization header.</param>
/// <param name="ExpiresAt">When the token stops being valid.</param>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
