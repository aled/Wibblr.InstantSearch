using System;
using System.Collections.Generic;

namespace Wibblr.InstantSearch
{
    public class SearchResult
    {
        public TimeSpan TotalSearchTime;
        public IList<SearchResultItem> SearchResultExactMatchItems;
        public IList<SearchResultItem> SearchResultAlternativeMatchItems;
    }
}
