// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using Sarafan.Core.Authentication;

namespace Sarafan.Core.Tests;

[TestFixture]
public sealed class VerificationAttemptStoreTests
{
    [Test]
    public void TryConsume_BoundsBucketsAndEvictsExpiredEntries()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 8, 30, 0, 0, 0, TimeSpan.Zero));
        var store = new VerificationAttemptStore(timeProvider);
        var window = TimeSpan.FromMinutes(15);

        for (var index = 0; index < 10_000; index++)
        {
            Assert.That(store.TryConsume($"phone:{index}", 3, window), Is.True);
        }

        Assert.That(store.TryConsume("over-capacity", 3, window), Is.False);

        timeProvider.Advance(window);
        Assert.That(store.TryConsume("after-expiry", 3, window), Is.True);
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public void Advance(TimeSpan duration) => now += duration;
    }
}
