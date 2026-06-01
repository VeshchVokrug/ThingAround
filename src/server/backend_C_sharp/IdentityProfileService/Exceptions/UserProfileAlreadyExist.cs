namespace IdentityProfileService.Exceptions;

public class UserProfileAlreadyExist : Exception
{
    public UserProfileAlreadyExist(Guid id) : base($"User profile with id {id} is already exist.") { }
}