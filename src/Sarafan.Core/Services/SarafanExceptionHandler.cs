// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using Microsoft.AspNetCore.Diagnostics;

using Sarafan.Core.Observability;

namespace Sarafan.Core.Services;

public sealed class SarafanExceptionHandler(
    SarafanProblemDetailsFactory problemDetailsFactory,
    ILogger<SarafanExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var (statusCode, code) = exception switch
        {
            ServiceException serviceException => (serviceException.StatusCode, serviceException.Code),
            BadHttpRequestException badRequest => (
                badRequest.StatusCode,
                SarafanProblemDetailsFactory.CodeForStatus(badRequest.StatusCode)),
            _ => (StatusCodes.Status500InternalServerError, "internal_error")
        };

        if (exception is not ServiceException and not BadHttpRequestException)
        {
            SarafanEvents.UnhandledException(logger, code, exception);
        }

        await problemDetailsFactory.WriteAsync(httpContext, statusCode, code, cancellationToken);
        return true;
    }
}
