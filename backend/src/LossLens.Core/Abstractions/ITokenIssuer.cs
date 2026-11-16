using LossLens.Core.Models;

namespace LossLens.Core.Abstractions;

/// <summary>Creates the access token of a signed-in user.</summary>
public interface ITokenIssuer
{
    AccessToken Issue(User user);
}
