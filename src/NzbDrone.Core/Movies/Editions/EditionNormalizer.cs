using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Movies.Editions
{
    // Reduces edition text ("Director's Cut", "Directors.Cut", "Extended Edition") to a comparable key.
    // An empty key means the standard edition.
    public static class EditionNormalizer
    {
        private static readonly Regex WordSplitRegex = new Regex(@"[^a-z0-9]+", RegexOptions.Compiled);
        private static readonly Regex OrdinalRegex = new Regex(@"^(\d+)(st|nd|rd|th)$", RegexOptions.Compiled);

        // Words that describe the kind of release rather than which cut it is.
        private static readonly HashSet<string> NoiseWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "cut", "edition", "version", "the"
        };

        // Words that don't make a release a different edition of the film.
        // A release tagged only with these belongs to the standard edition.
        private static readonly HashSet<string> StandardWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "theatrical", "remastered", "restored"
        };

        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "director", "directors" },
            { "dc", "directors" },
            { "collector", "collectors" }
        };

        public static string Normalize(string edition)
        {
            return string.Join(" ", GetTokens(edition));
        }

        public static IReadOnlyList<string> GetTokens(string edition)
        {
            if (edition.IsNullOrWhiteSpace())
            {
                return Array.Empty<string>();
            }

            var text = edition.ToLowerInvariant().Replace("'", "").Replace("’", "");

            return WordSplitRegex.Split(text)
                .Where(w => w.IsNotNullOrWhiteSpace())
                .Select(w => OrdinalRegex.Replace(w, "$1"))
                .Select(w => Aliases.TryGetValue(w, out var alias) ? alias : w)
                .Where(w => !NoiseWords.Contains(w) && !StandardWords.Contains(w))
                .Distinct()
                .OrderBy(w => w, StringComparer.Ordinal)
                .ToList();
        }

        public static bool IsStandard(string edition)
        {
            return GetTokens(edition).Count == 0;
        }

        // Finds the tracked edition a release's edition text belongs to.
        // An exact key match wins; otherwise the most specific edition whose words are all present
        // in the release's edition (e.g. "Extended Uncut" -> "Extended"), provided that choice is unambiguous.
        public static MovieEdition FindMatch(IEnumerable<MovieEdition> editions, string edition)
        {
            var tokens = GetTokens(edition);

            if (tokens.Count == 0)
            {
                return null;
            }

            var candidates = editions.Select(e => new { Edition = e, Tokens = GetTokens(e.Name) })
                .Where(c => c.Tokens.Count > 0)
                .ToList();

            var exact = candidates.FirstOrDefault(c => c.Tokens.SequenceEqual(tokens));

            if (exact != null)
            {
                return exact.Edition;
            }

            var partial = candidates.Where(c => c.Tokens.All(tokens.Contains))
                .GroupBy(c => c.Tokens.Count)
                .OrderByDescending(g => g.Key)
                .FirstOrDefault();

            if (partial == null || partial.Count() > 1)
            {
                return null;
            }

            return partial.Single().Edition;
        }
    }
}
