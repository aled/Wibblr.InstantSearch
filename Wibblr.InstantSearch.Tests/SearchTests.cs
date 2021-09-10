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
            Utils.GenerateSubstrings(Utils.NormalizeString("asdf"), 2).Select(x => x.ToString()).Should().BeEquivalentTo(new[] { "as", "sd", "df" });
        }

        [Fact]
        public void SearchTest()
        {
            var e = new SearchIndex();
  
            e.Add(1, "a1sdfasdf qwer zzzzz");
            e.Add(3, "assdfa1sdf");
            e.Add(4, "asfdfas2df");
            e.Add(5, "assdfasfdf");
            e.Add(6, "asfdfassdf");
            e.Add(7, "assdfafsdf");
            e.Add(8, "afsdfafsdf");

            var result = e.Search("zzzzz qwe");
            result.SearchResultItems.First().Should().BeEquivalentTo(new SearchResultItem { Order = 0, Score = 100, Id = 1, OriginalValue = "a1sdfasdf qwer zzzzz" });
        }
    }
}
