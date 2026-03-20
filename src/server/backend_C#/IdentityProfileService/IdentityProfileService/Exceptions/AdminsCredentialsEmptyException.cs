namespace IdentityProfileService.Exceptions;

public class AdminsCredentialsEmptyException : Exception
{
    private const string DefaultMessage = "The list of administrator credentials is empty.";

    public AdminsCredentialsEmptyException() : base(DefaultMessage) {}
    
    public AdminsCredentialsEmptyException(Exception innerException) : base(DefaultMessage, innerException) {}
}