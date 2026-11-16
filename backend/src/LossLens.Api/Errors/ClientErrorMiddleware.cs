using LossLens.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace LossLens.Api.Errors;

/// <summary>
/// Turns the client mistakes raised by the application layer into RFC 7807 problem details:
/// <see cref="ValidationException"/> becomes 400, <see cref="ForbiddenException"/> becomes 403, <see cref="UnauthorizedException"/> becomes 401.
/// Other exceptions pass through to the exception handler (500). Placed inside the exception handler, so
/// these expected errors are answered quietly instead of being logged as unhandled.
/// </summary>
public sealed class ClientErrorMiddleware(RequestDelegate next, IProblemDetailsService problemDetails)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted && StatusOf(ex) is not null)
        {
            var status = StatusOf(ex)!.Value;
            context.Response.Clear();
            context.Response.StatusCode = status;

            await problemDetails.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails { Status = status, Detail = ex.Message }
            });
        }
    }

    private static int? StatusOf(Exception exception) => exception switch
    {
        ValidationException => StatusCodes.Status400BadRequest,
        ForbiddenException => StatusCodes.Status403Forbidden,
        UnauthorizedException => StatusCodes.Status401Unauthorized,
        _ => null
    };
}
