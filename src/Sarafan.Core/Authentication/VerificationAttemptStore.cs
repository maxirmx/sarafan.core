// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using System.Collections.Concurrent;

namespace Sarafan.Core.Authentication;

public sealed class VerificationAttemptStore(TimeProvider timeProvider)
{
    private const int CleanupInterval = 128;
    private const int MaxBuckets = 10_000;
    private readonly ConcurrentDictionary<string, AttemptWindow> _windows = new(StringComparer.Ordinal);
    private long _operations;

    public bool TryConsume(string key, int limit, TimeSpan window)
    {
        var now = timeProvider.GetUtcNow();
        if (Interlocked.Increment(ref _operations) % CleanupInterval == 0 || _windows.Count >= MaxBuckets)
        {
            RemoveExpired(now, window);
        }

        if (_windows.Count >= MaxBuckets && !_windows.ContainsKey(key))
        {
            return false;
        }

        var bucket = _windows.GetOrAdd(key, _ => new AttemptWindow());

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
            bucket.LastTouched = now;
            return true;
        }
    }

    private void RemoveExpired(DateTimeOffset now, TimeSpan window)
    {
        foreach (var pair in _windows)
        {
            lock (pair.Value.Gate)
            {
                if (now - pair.Value.LastTouched >= window)
                {
                    _windows.TryRemove(pair);
                }
            }
        }
    }

    private sealed class AttemptWindow
    {
        public object Gate { get; } = new();
        public Queue<DateTimeOffset> Attempts { get; } = new();
        public DateTimeOffset LastTouched { get; set; }
    }
}
