namespace IdentityProfileService.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(Guid id) : base($"User with id {id} was not found.") {}
    
    public UserNotFoundException(string email) : base($"User with email {email} was not found.") {}
    
    public UserNotFoundException(Guid id, Exception innerException) : base($"User with id {id} was not found.",innerException) {}
}