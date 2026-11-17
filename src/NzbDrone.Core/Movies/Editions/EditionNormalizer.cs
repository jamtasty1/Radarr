using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Movies.Editions
{
    // Reduces edition text ("Director's Cut", "Directors.Cut", "Extended Edition") to a comparable key.
    // An empty key means the standard edition.
    public static class EditionNormalizer
    {
        private static readonly Regex WordSplitRegex = new Regex(@"[^a-z0-9]+", RegexOptions.Compiled);
        private static readonly Regex OrdinalRegex = new Regex(@"^(\d+)(st|nd|rd|th)$", RegexOptions.Compiled);

        // A release year, not part of a longer number or a resolution such as 2160p.
        private static readonly Regex YearRegex = new Regex(@"(?<![0-9a-z])(?:19|20)\d{2}(?![0-9a-z])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // A trailing "-GROUP"; brackets and braces are excluded so tags like "{edition-Redux}" survive.
        private static readonly Regex ReleaseGroupSuffixRegex = new Regex(@"-[^-.\s\[\](){}]+$", RegexOptions.Compiled);

        // Words that describe the kind of release rather than which cut it is.
        private static readonly HashSet<string> NoiseWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "cut", "edition", "version", "the"
        };

        // Words that don't make a release a different edition of the film.
        // A release tagged only with these belongs to the standard edition.
        private static readonly HashSet<string> StandardWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "theatrical", "remastered", "restored", "standard"
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

        // Finds a tracked edition named in a release or file name, for edition names the parser
        // doesn't recognise (e.g. "Redux", "Black and Chrome"). The release group and the words of the
        // movie's own titles are ignored, so "Final" in "Final Destination" doesn't count. The most
        // specific edition whose words all appear wins, provided that choice is unambiguous.
        public static MovieEdition FindInTitle(IEnumerable<MovieEdition> editions, string title, IEnumerable<string> movieTitles = null)
        {
            var tokens = GetTitleTokens(title, movieTitles);

            if (tokens.Count == 0)
            {
                return null;
            }

            var best = editions.Select(e => new { Edition = e, Tokens = GetTokens(e.Name) })
                .Where(c => c.Tokens.Count > 0 && c.Tokens.All(tokens.Contains))
                .GroupBy(c => c.Tokens.Count)
                .OrderByDescending(g => g.Key)
                .FirstOrDefault();

            if (best == null || best.Count() > 1)
            {
                return null;
            }

            return best.Single().Edition;
        }

        // Words after the release year (where scene names put the edition) count in full. Words
        // before it count only when they aren't part of the movie's title, e.g. "Apocalypse.Now.Redux.1979".
        private static HashSet<string> GetTitleTokens(string title, IEnumerable<string> movieTitles)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);

            if (title.IsNullOrWhiteSpace())
            {
                return tokens;
            }

            var text = ReleaseGroupSuffixRegex.Replace(FileExtensions.RemoveFileExtension(title.Trim()), string.Empty);
            var titleWords = new HashSet<string>((movieTitles ?? Enumerable.Empty<string>()).SelectMany(GetTokens), StringComparer.Ordinal);
            var year = YearRegex.Matches(text).LastOrDefault();

            var before = year == null ? text : text.Substring(0, year.Index);
            var after = year == null ? string.Empty : text.Substring(year.Index + year.Length);

            tokens.UnionWith(GetTokens(after));

            // Without the movie's titles, words before the year can't be told apart from the title.
            if (titleWords.Count > 0)
            {
                tokens.UnionWith(GetTokens(before).Where(t => !titleWords.Contains(t)));
            }

            return tokens;
        }
    }
}
