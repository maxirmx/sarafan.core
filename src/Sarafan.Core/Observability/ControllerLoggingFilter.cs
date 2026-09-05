// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Sarafan.Core.Observability;

public sealed class ControllerLoggingFilter(ILogger<ControllerLoggingFilter> logger) : IAsyncActionFilter, IOrderedFilter
{
    // Include validation short circuits as well as actions that run normally.
    public int Order => int.MinValue;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var descriptor = (ControllerActionDescriptor)context.ActionDescriptor;
        var operation = $"{descriptor.ControllerTypeInfo.FullName}.{descriptor.MethodInfo.Name}";
        OperationLogging.Enter(logger, operation, () => LogValueSummary.Inputs(
            context.ActionArguments.Select(argument => (argument.Key, argument.Value)).ToArray()));

        ActionExecutedContext executed;
        try
        {
            executed = await next();
        }
        catch (Exception exception)
        {
            OperationLogging.Failed(logger, operation, exception, context.HttpContext.RequestAborted);
            throw;
        }

        if (executed.Exception is { } failure && !executed.ExceptionHandled)
        {
            OperationLogging.Failed(logger, operation, failure, context.HttpContext.RequestAborted);
        }
        else
        {
            OperationLogging.Exit(logger, operation, executed.Result);
        }
    }
}
