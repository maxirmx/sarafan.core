// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

using Sarafan.Core.Services;

namespace Sarafan.Core.Controllers;

[ApiController]
public abstract class SarafanControllerBase : ControllerBase
{
    protected ActionResult ServiceProblem(ServiceException exception)
    {
        var details = new ProblemDetails
        {
            Status = exception.StatusCode,
            Title = exception.Message,
            Type = $"https://sarafan.sw.consulting/problems/{exception.Code}"
        };
        details.Extensions["code"] = exception.Code;
        return StatusCode(exception.StatusCode, details);
    }

    protected int CurrentCustomerId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var customerId)
            ? customerId
            : throw new ServiceException(
                StatusCodes.Status401Unauthorized,
                "invalid_access_token",
                "The access token does not identify a customer");
    }

    protected string RemoteAddress()
        => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
