using System;
using System.Linq;
using System.Text;

using FluentAssertions;

using Xunit;

namespace Wibblr.InstantSearch.Tests
{
    public class SearchTests
    {
        [Fact]
        public void NormalizeTest()
        {
            Encoding.ASCII.GetString(Utils.NormalizeString("asdf")).Should().Be("asdf");
            Encoding.ASCII.GetString(Utils.NormalizeString("\u00E9")).Should().Be("e");
        }

        [Fact]
        public void TrigramGenerateTest()
        {
            //Utils.Generate(Utils.NormalizeString("asdf"), 2).Select(x => x.ToString()).Should().BeEquivalentTo(new[] { "as", "sd", "df" });
        }

        [Fact]
        public void SearchTest()
        {
            var e = new SearchIndex();
  
            e.Add(1, "a1sdfasdf qwert zzzzz");
            e.Add(3, "assdfa1sdf");
            e.Add(4, "asfdfas2df");
            e.Add(5, "assdqwefdf");
            e.Add(6, "asfdfassdf");
            e.Add(7, "assdfafsdf");
            e.Add(8, "afsdfafsdf");

            var result = e.Search("zzzzz qwer");

            //Console.WriteLine("search: " + searchTerm + "; " + result.SearchResultExactMatchItems.Count + " exact results " + result.SearchResultAlternativeMatchItems.Count + " non-exact results in " + result.TotalSearchTime.TotalMilliseconds.ToString() + "ms");
            Console.WriteLine("Exact");
            Console.WriteLine(string.Join(",", result.SearchResultExactMatchItems.Select(x => x.ToString())));
            Console.WriteLine("NonExact");
            Console.WriteLine(string.Join(",", result.SearchResultAlternativeMatchItems.Select(x => x.ToString())));

            result.SearchResultExactMatchItems.Count().Should().Be(1);
            result.SearchResultExactMatchItems.First().Should().BeEquivalentTo(new SearchResultItem { Order = 0, Score = 100, Id = 1, OriginalValue = "a1sdfasdf qwert zzzzz" });

            // There is an alternative (non-exact) match as the 'qwe' in the search term matches an item.
            result.SearchResultAlternativeMatchItems.Count().Should().Be(1);
            result.SearchResultAlternativeMatchItems.First().Should().BeEquivalentTo(new SearchResultItem { Order = 0, Score = 33, Id = 5, OriginalValue = "assdqwefdf" });
        }
    }
}
