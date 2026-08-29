// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using Microsoft.Extensions.Options;

namespace Sarafan.Core.Authentication;

public interface IVerificationCodeProvider
{
    bool IsAvailable { get; }
    Task RequestCodeAsync(string phone, CancellationToken cancellationToken);
    Task<bool> VerifyCodeAsync(string phone, string? code, CancellationToken cancellationToken);
}

public sealed class FixedVerificationCodeProvider(IOptions<AuthenticationOptions> options) : IVerificationCodeProvider
{
    private readonly AuthenticationOptions _options = options.Value;

    public bool IsAvailable => _options.AllowFixedCode;

    public Task RequestCodeAsync(string phone, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return IsAvailable
            ? Task.CompletedTask
            : Task.FromException(new InvalidOperationException("No verification-code provider is configured"));
    }

    public Task<bool> VerifyCodeAsync(string phone, string? code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(IsAvailable && string.Equals(code?.Trim(), "1111", StringComparison.Ordinal));
    }
}
