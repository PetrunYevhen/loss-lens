using LossLens.Core.Abstractions;
using LossLens.Core.Models;
using LossLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LossLens.Infrastructure.Repositories;

public sealed class UserRepository(LossLensDbContext context) : IUserRepository
{
    public Task<User?> FindByUsernameAsync(string normalizedUsername, CancellationToken ct = default) =>
        context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == normalizedUsername, ct);
}
