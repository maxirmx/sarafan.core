// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

namespace Sarafan.Core.Models;

public sealed class CustomerConsent
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public ConsentType Type { get; set; }
    public required string DocumentVersion { get; set; }
    public DateTimeOffset AcceptedAt { get; set; }
}
