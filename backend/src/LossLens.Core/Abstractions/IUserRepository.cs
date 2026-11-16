using LossLens.Core.Models;

namespace LossLens.Core.Abstractions;

public interface IUserRepository
{
    /// <summary>Finds a user by the normalized user name (see <see cref="User.Normalize"/>), or null.</summary>
    Task<User?> FindByUsernameAsync(string normalizedUsername, CancellationToken ct = default);

    /// <summary>Finds a user by id (the <c>sub</c> claim of an access token), or null.</summary>
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
}
