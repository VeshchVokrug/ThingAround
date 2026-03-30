namespace IdentityProfileService.Exceptions;

public class UserAccountBlocked : Exception
{
    public UserAccountBlocked(Guid id, string email, string reason) : base($"User {email} with id {id} has been blocked. Reason: {reason}.") {}
}