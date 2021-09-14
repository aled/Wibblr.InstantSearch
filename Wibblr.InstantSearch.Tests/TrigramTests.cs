
using System.Collections.Generic;

using FluentAssertions;

using Xunit;

namespace Wibblr.InstantSearch.Tests
{
    public class TrigramTests
    {
        private IEnumerable<char> Base36Symbols()
        {
            for (char i = '0'; i <= '9'; i++)
                yield return i;

            for (char i = 'a'; i <= 'z'; i++)
                yield return i;
        }

        [Fact]
        public void CanEncodeAndDecode()
        {
            foreach (var c0 in Base36Symbols())
                foreach (var c1 in Base36Symbols())
                    foreach (var c2 in Base36Symbols())
                    {
                        var trigram = new Trigram((byte)c0, (byte)c1, (byte)c2);
                        trigram.ToString().Should().Be(new string(new [] { c0, c1, c2 }));
                    }
        }
    }
}
