// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Sarafan.Core.Authentication;
using Sarafan.Core.Data;
using Sarafan.Core.Models;
using Sarafan.Core.RestModels;

namespace Sarafan.Core.Services;

public sealed record AuthenticationSession(AuthenticationSessionDto Response, string RefreshToken);

public sealed class AuthenticationService(
    AppDbContext database,
    IPhoneNormalizer phoneNormalizer,
    IVerificationCodeProvider codeProvider,
    VerificationAttemptStore attemptStore,
    JwtTokenService tokenService,
    IOptions<AuthenticationOptions> options,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(15);
    private readonly AuthenticationOptions _options = options.Value;

    public async Task RequestCodeAsync(
        RequestCodeRequest request,
        string remoteAddress,
        CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.Phone);
        ValidatePurpose(request.Purpose);
        CheckAttemptLimit($"request:phone:{phone}", 3);
        CheckAttemptLimit($"request:ip:{remoteAddress}", 20);

        await codeProvider.RequestCodeAsync(phone, cancellationToken);
    }

    public async Task<AuthenticationSession> VerifyCodeAsync(
        VerifyCodeRequest request,
        string remoteAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.Phone);
        var purpose = ValidatePurpose(request.Purpose);
        CheckAttemptLimit($"verify:phone:{phone}", 5);
        CheckAttemptLimit($"verify:ip:{remoteAddress}", 30);

        if (!await codeProvider.VerifyCodeAsync(phone, request.Code, cancellationToken))
        {
            throw new ServiceException(
                StatusCodes.Status401Unauthorized,
                "invalid_code");
        }

        return purpose == "register"
            ? await RegisterAsync(phone, request, remoteAddress, userAgent, cancellationToken)
            : await LoginAsync(phone, remoteAddress, userAgent, cancellationToken);
    }

    public async Task<AuthenticationSession> RefreshAsync(
        string rawToken,
        string remoteAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var tokenHash = JwtTokenService.HashRefreshToken(rawToken);
        var now = timeProvider.GetUtcNow();
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        var current = await database.RefreshSessions
            .Include(item => item.Customer)
            .ThenInclude(item => item.Profile)
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (current is null)
        {
            throw InvalidRefreshToken();
        }

        if (current.RevokedAt is not null || current.ReplacedByTokenHash is not null || current.ExpiresAt <= now)
        {
            await RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw InvalidRefreshToken();
        }

        var nextRawToken = JwtTokenService.CreateRefreshToken();
        var nextHash = JwtTokenService.HashRefreshToken(nextRawToken);
        current.RevokedAt = now;
        current.ReplacedByTokenHash = nextHash;
        database.RefreshSessions.Add(CreateRefreshSession(
            current.Customer,
            current.FamilyId,
            nextHash,
            remoteAddress,
            userAgent,
            now));

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            var reused = await database.RefreshSessions
                .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
            if (reused is not null)
            {
                await RevokeFamilyAsync(reused.FamilyId, now, cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
            }

            throw InvalidRefreshToken();
        }

        var hasPhoto = await database.CustomerPhotos
            .AnyAsync(item => item.CustomerId == current.CustomerId, cancellationToken);
        return CreateSession(current.Customer, hasPhoto, nextRawToken);
    }

    public async Task LogoutAsync(string? rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return;
        }

        var hash = JwtTokenService.HashRefreshToken(rawToken);
        var current = await database.RefreshSessions
            .SingleOrDefaultAsync(item => item.TokenHash == hash, cancellationToken);
        if (current is null)
        {
            return;
        }

        await RevokeFamilyAsync(current.FamilyId, timeProvider.GetUtcNow(), cancellationToken);
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthenticationSession> RegisterAsync(
        string phone,
        VerifyCodeRequest request,
        string remoteAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (!request.TermsAccepted || !request.PersonalDataAccepted)
        {
            throw new ServiceException(
                StatusCodes.Status400BadRequest,
                "consent_required");
        }

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (await database.Customers.AnyAsync(item => item.Phone == phone, cancellationToken))
        {
            throw new ServiceException(
                StatusCodes.Status409Conflict,
                "account_exists");
        }

        var now = timeProvider.GetUtcNow();
        var customer = new Customer
        {
            Phone = phone,
            CreatedAt = now,
            UpdatedAt = now,
            Profile = new CustomerProfile()
        };
        customer.Consents.Add(new CustomerConsent
        {
            Type = ConsentType.Terms,
            DocumentVersion = _options.TermsVersion,
            AcceptedAt = now
        });
        customer.Consents.Add(new CustomerConsent
        {
            Type = ConsentType.PersonalData,
            DocumentVersion = _options.PersonalDataVersion,
            AcceptedAt = now
        });

        var rawRefreshToken = JwtTokenService.CreateRefreshToken();
        customer.RefreshSessions.Add(CreateRefreshSession(
            customer,
            Guid.NewGuid(),
            JwtTokenService.HashRefreshToken(rawRefreshToken),
            remoteAddress,
            userAgent,
            now));
        database.Customers.Add(customer);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ServiceException(
                StatusCodes.Status409Conflict,
                "account_exists");
        }

        return CreateSession(customer, false, rawRefreshToken);
    }

    private async Task<AuthenticationSession> LoginAsync(
        string phone,
        string remoteAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var customer = await database.Customers
            .Include(item => item.Profile)
            .SingleOrDefaultAsync(item => item.Phone == phone, cancellationToken);
        if (customer is null || customer.State == CustomerState.Disabled)
        {
            throw new ServiceException(
                StatusCodes.Status401Unauthorized,
                "login_failed");
        }

        var now = timeProvider.GetUtcNow();
        var rawRefreshToken = JwtTokenService.CreateRefreshToken();
        database.RefreshSessions.Add(CreateRefreshSession(
            customer,
            Guid.NewGuid(),
            JwtTokenService.HashRefreshToken(rawRefreshToken),
            remoteAddress,
            userAgent,
            now));
        await database.SaveChangesAsync(cancellationToken);
        var hasPhoto = await database.CustomerPhotos
            .AnyAsync(item => item.CustomerId == customer.Id, cancellationToken);
        return CreateSession(customer, hasPhoto, rawRefreshToken);
    }

    private AuthenticationSession CreateSession(Customer customer, bool hasPhoto, string refreshToken)
    {
        var accessToken = tokenService.CreateAccessToken(customer);
        return new AuthenticationSession(
            new AuthenticationSessionDto(
                accessToken.Token,
                accessToken.ExpiresAt,
                CustomerDto.From(customer, hasPhoto)),
            refreshToken);
    }

    private RefreshSession CreateRefreshSession(
        Customer customer,
        Guid familyId,
        string tokenHash,
        string remoteAddress,
        string? userAgent,
        DateTimeOffset now) => new()
        {
            Customer = customer,
            FamilyId = familyId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_options.RefreshTokenDays),
            CreatedByIp = Limit(remoteAddress, 64),
            UserAgent = Limit(userAgent, 256)
        };

    private async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var sessions = await database.RefreshSessions
            .Where(item => item.FamilyId == familyId && item.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAt = now;
        }
    }

    private string NormalizePhone(string? phone)
    {
        if (!phoneNormalizer.TryNormalize(phone, out var normalized))
        {
            throw new ServiceException(
                StatusCodes.Status400BadRequest,
                "invalid_phone");
        }

        return normalized;
    }

    private static string ValidatePurpose(string purpose)
    {
        var normalized = purpose.Trim().ToLowerInvariant();
        if (normalized is not ("register" or "login"))
        {
            throw new ServiceException(
                StatusCodes.Status400BadRequest,
                "invalid_purpose");
        }

        return normalized;
    }

    private void CheckAttemptLimit(string key, int limit)
    {
        if (!attemptStore.TryConsume(key, limit, AttemptWindow))
        {
            throw new ServiceException(
                StatusCodes.Status429TooManyRequests,
                "rate_limited");
        }
    }

    private static ServiceException InvalidRefreshToken() => new(
        StatusCodes.Status401Unauthorized,
        "invalid_refresh_token");

    private static string? Limit(string? value, int length)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, length)];
}
