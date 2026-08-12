using Microsoft.AspNetCore.Antiforgery;

namespace Azulyoro.Api.Configuration;

/// <summary>Validates the antiforgery (CSRF) token on mutating endpoints.
/// The SPA reads the token from <c>GET /api/auth/csrf</c> and echoes it in
/// the <c>X-XSRF-TOKEN</c> header on subsequent POSTs.</summary>
public sealed class AntiforgeryEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var antiforgery = http.RequestServices.GetRequiredService<IAntiforgery>();

        try
        {
            await antiforgery.ValidateRequestAsync(http);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                detail: "Invalid or missing CSRF token.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return await next(context);
    }
}
