using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Movies.Editions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MovieTests.EditionTests
{
    [TestFixture]
    public class EditionNormalizerFixture : CoreTest
    {
        [TestCase("Director's Cut", "Directors Cut")]
        [TestCase("Directors.Cut", "Director's Cut")]
        [TestCase("Director’s Cut", "DIRECTORS CUT")]
        [TestCase("Extended", "Extended Edition")]
        [TestCase("Extended Cut", "Extended Edition")]
        [TestCase("IMAX", "imax")]
        [TestCase("25th Anniversary", "25 Anniversary Edition")]
        [TestCase("Extended Directors Cut", "Directors Cut Extended")]
        [TestCase("Director's Cut Remastered", "Directors Cut")]
        public void should_treat_as_same_edition(string first, string second)
        {
            EditionNormalizer.Normalize(first).Should().Be(EditionNormalizer.Normalize(second));
        }

        [TestCase("Extended", "Director's Cut")]
        [TestCase("Unrated", "Uncut")]
        [TestCase("Extended", "Extended Directors Cut")]
        public void should_treat_as_different_editions(string first, string second)
        {
            EditionNormalizer.Normalize(first).Should().NotBe(EditionNormalizer.Normalize(second));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Theatrical")]
        [TestCase("Theatrical Cut")]
        [TestCase("Remastered")]
        [TestCase("Restored")]
        public void should_be_standard_edition(string edition)
        {
            EditionNormalizer.IsStandard(edition).Should().BeTrue();
        }

        [TestCase("Extended")]
        [TestCase("IMAX")]
        [TestCase("Open Matte")]
        public void should_not_be_standard_edition(string edition)
        {
            EditionNormalizer.IsStandard(edition).Should().BeFalse();
        }

        private static List<MovieEdition> Editions(params string[] names)
        {
            var editions = new List<MovieEdition>();

            for (var i = 0; i < names.Length; i++)
            {
                editions.Add(new MovieEdition { Id = i + 1, Name = names[i] });
            }

            return editions;
        }

        [TestCase("Directors.Cut", "Director's Cut")]
        [TestCase("Extended Edition", "Extended")]
        [TestCase("IMAX", "IMAX")]
        [TestCase("Extended Uncut", "Extended")]
        public void should_find_matching_edition(string releaseEdition, string expected)
        {
            var editions = Editions("Extended", "Director's Cut", "IMAX");

            EditionNormalizer.FindMatch(editions, releaseEdition).Name.Should().Be(expected);
        }

        [Test]
        public void should_prefer_exact_match_over_partial_match()
        {
            var editions = Editions("Extended", "Extended Director's Cut");

            EditionNormalizer.FindMatch(editions, "Extended Directors Cut").Name.Should().Be("Extended Director's Cut");
        }

        [Test]
        public void should_not_match_when_partial_match_is_ambiguous()
        {
            var editions = Editions("Extended", "Director's Cut");

            EditionNormalizer.FindMatch(editions, "Extended Directors Cut").Should().BeNull();
        }

        [TestCase("Unrated")]
        [TestCase(null)]
        [TestCase("Theatrical")]
        public void should_not_match_untracked_or_standard_edition(string releaseEdition)
        {
            var editions = Editions("Extended", "Director's Cut");

            EditionNormalizer.FindMatch(editions, releaseEdition).Should().BeNull();
        }

        [TestCase("Apocalypse.Now.1979.Redux.720p.WEB-DL.x264-GRP", "Redux")]
        [TestCase("Apocalypse.Now.Redux.1979.1080p.BluRay.x264-GRP", "Redux")]
        [TestCase("Mad.Max.Fury.Road.2015.Black.and.Chrome.Edition.1080p.BluRay.x264-GRP", "Black and Chrome")]
        [TestCase("Apocalypse Now (1979) {edition-Redux} [Bluray-1080p].mkv", "Redux")]
        [TestCase("Apocalypse.Now.1979.REDUX.2160p.UHD.BluRay.REMUX.HDR.HEVC-GRP.mkv", "Redux")]
        public void should_find_tracked_edition_named_in_title(string title, string expected)
        {
            var editions = Editions("Redux", "Black and Chrome");

            EditionNormalizer.FindInTitle(editions, title, new[] { "Apocalypse Now", "Mad Max: Fury Road" }).Name.Should().Be(expected);
        }

        [TestCase("Apocalypse.Now.1979.1080p.BluRay.x264-GRP")]
        [TestCase("Apocalypse.Now.1979.1080p.BluRay.x264-REDUX")]
        [TestCase(null)]
        public void should_not_find_edition_in_title_without_it(string title)
        {
            EditionNormalizer.FindInTitle(Editions("Redux"), title, new[] { "Apocalypse Now" }).Should().BeNull();
        }

        [Test]
        public void should_not_count_words_from_the_movie_title()
        {
            var editions = Editions("Final");

            EditionNormalizer.FindInTitle(editions, "Final.Destination.2000.1080p.BluRay.x264-GRP", new[] { "Final Destination" }).Should().BeNull();
        }

        [Test]
        public void should_only_search_after_year_when_movie_titles_are_unknown()
        {
            var editions = Editions("Redux");

            EditionNormalizer.FindInTitle(editions, "Apocalypse.Now.Redux.1979.1080p.BluRay.x264-GRP").Should().BeNull();
            EditionNormalizer.FindInTitle(editions, "Apocalypse.Now.1979.Redux.1080p.BluRay.x264-GRP").Name.Should().Be("Redux");
        }

        [Test]
        public void should_prefer_most_specific_edition_in_title()
        {
            var editions = Editions("Chrome", "Black and Chrome");

            EditionNormalizer.FindInTitle(editions, "Mad.Max.Fury.Road.2015.Black.and.Chrome.1080p-GRP").Name.Should().Be("Black and Chrome");
        }

        [Test]
        public void should_not_find_edition_in_title_when_ambiguous()
        {
            var editions = Editions("Redux", "Workprint");

            EditionNormalizer.FindInTitle(editions, "Apocalypse.Now.1979.Redux.Workprint.1080p-GRP").Should().BeNull();
        }
    }
}
