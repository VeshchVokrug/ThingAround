namespace Catalog.Contracts.Auth;

public interface IUserContext
{
    Guid UserId { get; }
    string Role { get; }
    bool IsAdmin { get; }
}