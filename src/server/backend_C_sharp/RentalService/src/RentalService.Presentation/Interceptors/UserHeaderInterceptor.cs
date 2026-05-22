using System.Security.Claims;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace RentalService.Presentation.Interceptors;

public class UserHeaderInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var userId = context.RequestHeaders.GetValue("x-user-id");
        var role = context.RequestHeaders.GetValue("x-user-role") ?? "Guest";

        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new[] { 
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role) 
            };
            var identity = new ClaimsIdentity(claims, "GatewayAuth");
            context.GetHttpContext().User = new ClaimsPrincipal(identity);
        }

        return await continuation(request, context);
    }
}