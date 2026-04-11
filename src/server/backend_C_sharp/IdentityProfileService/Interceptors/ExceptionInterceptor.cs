using Grpc.Core;
using Grpc.Core.Interceptors;
using IdentityProfileService.Exceptions;
using Microsoft.Extensions.Logging;

namespace IdentityProfileService.Interceptors;

public class ExceptionInterceptor : Interceptor
{
    private readonly ILogger<ExceptionInterceptor> _logger;

    public ExceptionInterceptor(ILogger<ExceptionInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error thrown during gRPC call: {Method}", context.Method);
            throw CreateRpcException(ex);
        }
    }

    private RpcException CreateRpcException(Exception exception)
    {
        return exception switch
        {
            InvalidTokenException => new RpcException(new Status(StatusCode.Unauthenticated, exception.Message)),
            SessionExpiredException => new RpcException(new Status(StatusCode.Unauthenticated, exception.Message)),
            InvalidCredentials => new RpcException(new Status(StatusCode.Unauthenticated, "Invalid username or password")),
            
            UserAccountBlocked => new RpcException(new Status(StatusCode.PermissionDenied, "Account is suspended")),
            
            UserNotFoundException => new RpcException(new Status(StatusCode.NotFound, exception.Message)),
            
            AdminsCredentialsEmptyException => new RpcException(new Status(StatusCode.InvalidArgument, "Admin credentials cannot be empty")),
            
            RegistrationException => new RpcException(new Status(StatusCode.AlreadyExists, exception.Message)),
            
            RpcException rpcEx => rpcEx,
            _ => new RpcException(new Status(StatusCode.Internal, "An internal server error occurred"))
        };
    }
}