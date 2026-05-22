using System.Security.Authentication;
using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using RentalService.Application.Exceptions;

namespace RentalService.Presentation.Interceptors;

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

    private static RpcException CreateRpcException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => new RpcException(new Status(StatusCode.InvalidArgument, validationException.Message)),
            ArgumentException argumentException => new RpcException(new Status(StatusCode.InvalidArgument, argumentException.Message)),
            ForbiddenOrNotFoundException => new RpcException(new Status(StatusCode.NotFound, exception.Message)),
            TimeoutException => new RpcException(new Status(StatusCode.Aborted, exception.Message)),
            AuthenticationException => new RpcException(new Status(StatusCode.Unauthenticated, exception.Message)),
            InvalidOperationException => new RpcException(new Status(StatusCode.Aborted, exception.Message)),
            RpcException rpcEx => rpcEx,
            _ => new RpcException(new Status(StatusCode.Internal, "An internal server error occurred"))
        };
    }
}

