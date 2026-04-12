using Gateway.Models;
using Grpc.Core;

namespace Gateway.Infrastructure.Middleware;

public sealed class GatewayExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GatewayExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
            await WriteFrameworkErrorIfNeededAsync(context);
        }
        catch (RpcException ex)
        {
            await WriteErrorAsync(context, MapGrpcStatusToHttpStatus(ex.StatusCode), ex.Status.Detail);
        }
        catch (BadHttpRequestException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Gateway internal error.");
        }
    }

    private static async Task WriteFrameworkErrorIfNeededAsync(HttpContext context)
    {
        var statusCode = context.Response.StatusCode;
        if (context.Response.HasStarted)
        {
            return;
        }

        if (statusCode < StatusCodes.Status400BadRequest)
        {
            return;
        }

        if (context.Response.ContentLength.GetValueOrDefault() > 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(context.Response.ContentType))
        {
            return;
        }

        var message = statusCode switch
        {
            StatusCodes.Status404NotFound => "Route not found.",
            StatusCodes.Status405MethodNotAllowed => "Method is not allowed for this route.",
            StatusCodes.Status401Unauthorized => "Authentication is required.",
            StatusCodes.Status403Forbidden => "Access denied.",
            _ => "Request failed on gateway."
        };

        await WriteErrorAsync(context, statusCode, message);
    }

    private static int MapGrpcStatusToHttpStatus(StatusCode grpcStatusCode)
    {
        return grpcStatusCode switch
        {
            StatusCode.Cancelled => 499,
            StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
            StatusCode.NotFound => StatusCodes.Status404NotFound,
            StatusCode.AlreadyExists => StatusCodes.Status409Conflict,
            StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
            StatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
            StatusCode.ResourceExhausted => StatusCodes.Status429TooManyRequests,
            StatusCode.Unimplemented => StatusCodes.Status501NotImplemented,
            StatusCode.Unavailable => StatusCodes.Status503ServiceUnavailable,
            StatusCode.DeadlineExceeded => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        var payload = new ApiErrorResponse(statusCode, message);

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(payload);
    }
}



