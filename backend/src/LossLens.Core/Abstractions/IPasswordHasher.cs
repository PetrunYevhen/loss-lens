namespace LossLens.Core.Abstractions;

public interface IPasswordHasher
{
    /// <summary>Hashes a password with a fresh random salt.</summary>
    string Hash(string password);

    /// <summary>
    /// Checks a password against a stored hash. A null or malformed hash gives false, and the null case still
    /// does the full amount of work, so the response time does not reveal that a user name does not exist.
    /// </summary>
    bool Verify(string password, string? hash);
}
