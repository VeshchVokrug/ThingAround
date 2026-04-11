namespace IdentityProfileService.Exceptions;

public class AuthException : Exception
{
    public AuthException(string message) : base(message) { }
}

public class InvalidTokenException : AuthException
{
    public InvalidTokenException() : base("Предоставлен невалидный или подделанный токен.") { }
}

public class SessionExpiredException : AuthException
{
    public SessionExpiredException() : base("Сессия истекла. Пожалуйста, войдите снова.") { }
}

public class InvalidCredentials : Exception
{
    public InvalidCredentials(string email) : base($"User {email} has invalid credentials.") {}
}

public class RegistrationException : Exception
{
    public IEnumerable<string> ErrorCodes { get; }

    public RegistrationException(string message) : base(message)
    {
        ErrorCodes = [];
    }

    public RegistrationException(string message, IEnumerable<string> errorCodes) : base(message)
    {
        ErrorCodes = errorCodes ?? [];
    }
    
    public string FullDetails => $"{Message} (Codes: {string.Join(", ", ErrorCodes)})";
}

public class AdminsCredentialsEmptyException : Exception
{
    private const string DefaultMessage = "The list of administrator credentials is empty.";

    public AdminsCredentialsEmptyException() : base(DefaultMessage) {}
    
    public AdminsCredentialsEmptyException(Exception innerException) : base(DefaultMessage, innerException) {}
}