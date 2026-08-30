// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Sarafan.Core.Services;

namespace Sarafan.Core.Authentication;

public sealed class SarafanJwtBearerEvents : JwtBearerEvents
{
    public override async Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        var factory = context.HttpContext.RequestServices
            .GetRequiredService<SarafanProblemDetailsFactory>();
        await factory.WriteAsync(
            context.HttpContext,
            StatusCodes.Status401Unauthorized,
            "invalid_access_token",
            context.HttpContext.RequestAborted);
    }

    public override async Task Forbidden(ForbiddenContext context)
    {
        var factory = context.HttpContext.RequestServices
            .GetRequiredService<SarafanProblemDetailsFactory>();
        await factory.WriteAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "access_denied",
            context.HttpContext.RequestAborted);
    }
}
