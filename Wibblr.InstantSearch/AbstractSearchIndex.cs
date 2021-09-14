using System;
using System.Collections.Generic;

namespace Wibblr.InstantSearch
{
    public abstract class AbstractSearchIndex
    {
        protected Dictionary<int, string> originalValues = new Dictionary<int, string>();

        abstract public SearchResult Search(string searchTerm);

        public SearchResult Scan(string searchTerm)
        {
            var words = searchTerm.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);

            var order = 0;
            var searchResult = new SearchResult();

            searchResult.SearchResultExactMatchItems = new List<SearchResultItem>();
            searchResult.SearchResultAlternativeMatchItems = new List<SearchResultItem>();

            foreach (var kv in originalValues)
            {
                var id = kv.Key;
                var value = kv.Value;

                var include = true;
                foreach (var word in words)
                {
                    // TODO: implement Contains(string, StringComparison.IgnoreCase)
                    //       which doesn't exist in .net standard 2.0
                    if (!value.Contains(word.ToLower()))
                    {
                        include = false;
                        break;
                    }
                }

                if (include)
                {
                    searchResult.SearchResultExactMatchItems.Add(new SearchResultItem
                    {
                        Order = order,
                        Id = id,
                        OriginalValue = value,
                        Score = 100
                    });

                    order++;
                }
            }
            return searchResult;
        }
    }
}
