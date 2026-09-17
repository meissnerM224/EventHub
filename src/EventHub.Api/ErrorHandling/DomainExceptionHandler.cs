using EventHub.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.ErrorHandling;

public class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var mapped = Map(exception);
        if (mapped is null)
            return false;

        var (status, title) = mapped.Value;

        logger.LogInformation(
            "Request rejected: {ExceptionType} → {StatusCode}. {ExceptionMessage}",
            exception.GetType().Name, status, exception.Message);

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message
            }
        });
    }

    private static (int Status, string Title)? Map(Exception exception) => exception switch
    {
        FullyBookedException => (StatusCodes.Status409Conflict, "Event is fully booked"),
        AlreadyExistException => (StatusCodes.Status409Conflict, "Resource already exists"),
        NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
        BusinessRuleException => (StatusCodes.Status400BadRequest, "Business rule violated"),
        ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
        UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
        _ => null
    };
}