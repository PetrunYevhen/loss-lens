using LossLens.Application.Security;

namespace LossLens.Api.Auth;

/// <summary>Builds the <see cref="UserContext"/> of the current request.</summary>
public interface IUserContextProvider
{
    Task<UserContext> GetCurrentAsync(CancellationToken ct = default);
}
