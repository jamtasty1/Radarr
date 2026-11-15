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
    }
}
