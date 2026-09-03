// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Sarafan.Core.Observability;

internal sealed class HumanReadableLogRecordProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        ArgumentNullException.ThrowIfNull(data);
        data.Exception = null;
        if (!string.IsNullOrWhiteSpace(data.FormattedMessage))
        {
            data.Body = OneLine(data.FormattedMessage);
        }
    }

    internal static string OneLine(string value)
        => value.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
}
