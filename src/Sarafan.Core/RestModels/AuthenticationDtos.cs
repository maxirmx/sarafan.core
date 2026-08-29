// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using System.ComponentModel.DataAnnotations;

namespace Sarafan.Core.RestModels;

public class RequestCodeRequest
{
    [Required]
    [StringLength(64)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(register|login)$", ErrorMessage = "Purpose must be register or login")]
    public string Purpose { get; set; } = string.Empty;
}

public sealed class VerifyCodeRequest : RequestCodeRequest
{
    [Required]
    [StringLength(16)]
    public string Code { get; set; } = string.Empty;

    public bool TermsAccepted { get; set; }
    public bool PersonalDataAccepted { get; set; }
}

public sealed record AuthenticationSessionDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    CustomerDto Customer);
