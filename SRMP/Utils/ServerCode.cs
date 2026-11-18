using System;
using System.Security.Cryptography;
using System.Text;

namespace SRMultiplayer
{
    /// <summary>
    /// The shareable lobby code format. Keep generation, UI and EOS validation
    /// on the same length and alphabet so a displayed code is always joinable.
    /// Two formats are accepted: SRMP's own 7-character code, and the 9-character
    /// SlimeBridge session code (Base58, case-sensitive: the same code Ranching
    /// Together shows in Slime Rancher 2), so one code joins both games.
    /// </summary>
    internal static class ServerCode
    {
        internal const int Length = 7;
        internal const int LinkedLength = 9;
        internal const int MaxLength = LinkedLength;
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string Base58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        /// <summary>Spaces and dashes removed, case kept (what the player is typing).</summary>
        internal static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var cleaned = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (char.IsWhiteSpace(character) || character == '-') continue;
                cleaned.Append(character);
            }
            return cleaned.ToString();
        }

        internal static string Normalize(string value)
        {
            var cleaned = Clean(value);
            return IsLinked(cleaned) ? cleaned : cleaned.ToUpperInvariant();
        }

        /// <summary>A 9-character SlimeBridge session code (the Ranching Together join code of the same session).</summary>
        internal static bool IsLinked(string value)
        {
            if (value == null || value.Length != LinkedLength) return false;
            foreach (char character in value)
                if (Base58.IndexOf(character) < 0) return false;
            return true;
        }

        internal static bool IsValid(string value)
        {
            if (IsLinked(value)) return true;
            if (value == null || value.Length != Length) return false;
            foreach (char character in value)
            {
                if ((character < 'A' || character > 'Z') && (character < '0' || character > '9'))
                    return false;
            }
            return true;
        }

        internal static string Generate()
        {
            // Rejection sampling avoids modulo bias while using only APIs
            // available on the game's .NET Framework target.
            var result = new char[Length];
            var bytes = new byte[Length * 2];
            using (var random = RandomNumberGenerator.Create())
            {
                int filled = 0;
                while (filled < result.Length)
                {
                    random.GetBytes(bytes);
                    foreach (byte candidate in bytes)
                    {
                        if (candidate >= 252) continue; // 7 * 36, evenly divisible into 256
                        result[filled++] = Alphabet[candidate % Alphabet.Length];
                        if (filled == result.Length) break;
                    }
                }
            }
            return new string(result);
        }
    }
}
