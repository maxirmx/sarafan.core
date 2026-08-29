// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using System.Collections.Concurrent;

namespace Sarafan.Core.Authentication;

public sealed class VerificationAttemptStore(TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<string, AttemptWindow> _windows = new(StringComparer.Ordinal);

    public bool TryConsume(string key, int limit, TimeSpan window)
    {
        var bucket = _windows.GetOrAdd(key, _ => new AttemptWindow());
        var now = timeProvider.GetUtcNow();

        lock (bucket.Gate)
        {
            while (bucket.Attempts.TryPeek(out var oldest) && now - oldest >= window)
            {
                bucket.Attempts.Dequeue();
            }

            if (bucket.Attempts.Count >= limit)
            {
                return false;
            }

            bucket.Attempts.Enqueue(now);
            return true;
        }
    }

    private sealed class AttemptWindow
    {
        public object Gate { get; } = new();
        public Queue<DateTimeOffset> Attempts { get; } = new();
    }
}
