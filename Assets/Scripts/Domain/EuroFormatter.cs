using System;

namespace PsyCurio.Shop.Domain
{
    /// <summary>
    /// Formats integer cents as a euro amount ("7,47 €", German style).
    /// Hand-rolled instead of CultureInfo so output is deterministic in tests
    /// and immune to ICU/globalization stripping under IL2CPP on Android.
    /// No thousands separator — shop prices here never reach four digits.
    /// </summary>
    public static class EuroFormatter
    {
        public static string Format(int cents)
        {
            if (cents < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cents), cents, "Amount must not be negative.");
            }

            return $"{cents / 100},{cents % 100:00} €";
        }
    }
}
