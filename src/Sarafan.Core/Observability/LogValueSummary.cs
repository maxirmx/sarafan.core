// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using Microsoft.AspNetCore.Mvc;

using Sarafan.Core.Authentication;
using Sarafan.Core.Models;
using Sarafan.Core.RestModels;
using Sarafan.Core.Services;

namespace Sarafan.Core.Observability;

internal static class LogValueSummary
{
    // Only these explicit projections may read values. Never serialize or call ToString on arbitrary input.
    internal static string Inputs(params (string Name, object? Value)[] values)
        => values.Length == 0 ? "none" : string.Join("; ", values.Select(value => $"{value.Name}={Describe(value.Value)}"));

    internal static string Describe(object? value) => value switch
    {
        null => "null",
        CancellationToken token => $"cancellation requested={token.IsCancellationRequested}",
        VerifyCodeRequest request => $"VerifyCodeRequest(purpose={Purpose(request.Purpose)}; phone/code/consents=[redacted])",
        RequestCodeRequest request => $"RequestCodeRequest(purpose={Purpose(request.Purpose)}; phone=[redacted])",
        CustomerProfileUpdateRequest => "CustomerProfileUpdateRequest([redacted])",
        CustomerDto => "CustomerDto([redacted])",
        Customer => "Customer([redacted])",
        AuthenticationSession => "AuthenticationSession(tokens/customer=[redacted])",
        AuthenticationSessionDto => "AuthenticationSessionDto(token/customer=[redacted])",
        AccessTokenResult => "AccessTokenResult(token/expiry=[redacted])",
        IFormFile => "file(content/metadata=[redacted])",
        FileResult => "file result(content/metadata=[redacted])",
        SarafanProblemDetails problem => $"problem(status={problem.Status}; details=[redacted])",
        ServiceStatus status when status.Service == "Sarafan.Core" && status.Status == "ok" && status.AppVersion == VersionInfo.AppVersion
            => $"ServiceStatus(name=Sarafan.Core; status=ok; version={VersionInfo.AppVersion})",
        ServiceStatus => "ServiceStatus([redacted])",
        ObjectResult result => $"status={result.StatusCode ?? StatusCodes.Status200OK}; output={Describe(result.Value)}",
        StatusCodeResult result => $"status={result.StatusCode}; no body",
        EmptyResult => "no body",
        OperationLogging.OperationCompleted => "completed; no return value",
        bool result => result ? "true" : "false",
        _ => "[redacted]"
    };

    private static string Purpose(string? purpose) => purpose?.Trim().ToLowerInvariant() switch
    {
        "register" => "register",
        "login" => "login",
        _ => "other"
    };
}
