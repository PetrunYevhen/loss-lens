using LossLens.Core.Models;

namespace LossLens.Core.Abstractions;

public interface IUserRepository
{
    /// <summary>Finds a user by the normalized user name (see <see cref="User.Normalize"/>), or null.</summary>
    Task<User?> FindByUsernameAsync(string normalizedUsername, CancellationToken ct = default);
}
