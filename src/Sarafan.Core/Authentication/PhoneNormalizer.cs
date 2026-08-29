// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

namespace Sarafan.Core.Authentication;

public interface IPhoneNormalizer
{
    bool TryNormalize(string? value, out string normalized);
}

public sealed class PhoneNormalizer : IPhoneNormalizer
{
    public bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        var plusCount = trimmed.Count(character => character == '+');
        var hasLeadingPlus = trimmed.StartsWith('+');
        if (plusCount > 1 || (plusCount == 1 && !hasLeadingPlus) ||
            trimmed.Any(character =>
                !IsAsciiDigit(character) &&
                !char.IsWhiteSpace(character) &&
                character is not ('+' or '(' or ')' or '-' or '.')))
        {
            return false;
        }

        var digits = new string(trimmed.Where(IsAsciiDigit).ToArray());
        if (digits.Length == 0 || digits.All(character => character == '0'))
        {
            return false;
        }

        if (digits.Length == 10 && !hasLeadingPlus)
        {
            digits = $"7{digits}";
        }
        else if (digits.Length == 11 && digits[0] == '8' && !hasLeadingPlus)
        {
            digits = $"7{digits[1..]}";
        }

        if (digits.Length is < 8 or > 15 || digits[0] == '0')
        {
            return false;
        }

        normalized = $"+{digits}";
        return true;
    }

    private static bool IsAsciiDigit(char character) => character is >= '0' and <= '9';
}
