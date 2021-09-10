using System;

using Xunit;
using FluentAssertions;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Wibblr.InstantSearch.Tests
{
    public class SearchTests
    {
        [Fact]
        public void Test1()
        {
            Encoding.ASCII.GetString(Utils.NormalizeString("asdf")).Should().Be("asdf");

            Encoding.ASCII.GetString(Utils.NormalizeString("\u00E9")).Should().Be("e");

            //new Engine().Add("customer", 1, new System.Collections.Generic.Dictionary<string, string> { { "firstName", "Fred" }, { "lastName", "Flintstone" } });
        }

        [Fact]
        public void Test2()
        {
            Utils.GenerateSubstrings(Utils.NormalizeString("asdf"), 2).Select(x => x.ToString()).Should().BeEquivalentTo(new[] { "as", "sd", "df" });
        }

        [Fact]
        public void Test4()
        {
            var e = new SearchIndex();
  
            e.Add(1, "a1sdfasdf qwer");
            e.Add(3, "assdfa1sdf");
            e.Add(4, "asfdfas2df");
            e.Add(5, "assdfasfdf");
            e.Add(6, "asfdfassdf");
            e.Add(7, "assdfafsdf");
            e.Add(8, "afsdfafsdf");

            var result = e.Search("df qwe");
            result.SearchResultItems.First().Should().BeEquivalentTo(new SearchResultItem { Order = 0, Score = 100, Id = 1, OriginalValue = "a1sdfasdf qwer" });

            //Console.WriteLine(string.Join("\n", result.Splits.Select(x => x.TotalMilliseconds.ToString() + "ms")));
        }
    }
}
