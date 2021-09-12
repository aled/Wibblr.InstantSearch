using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wibblr.InstantSearch
{
    /// <summary>
    /// Search engine that gives instant results.
    /// Intended to be used for search-as-you-type queries.
    /// 
    /// Performs only 'exact match' searches, though case and accent insensitive
    /// (all items are converted to a-z0-9 internally).
    /// 
    /// All 1, 2, and 3 letter searches are precomputed and stored 
    /// in memory if possible.
    /// 
    /// Searches greater than 4 letters are calculated on the fly
    /// </summary>   
    public class SearchIndex
    {
        int[] compressedTrigramCounts = new int[36 * 36 * 36];

        ISet<int>[] trigramArr = new LowMemorySet[36 * 36 * 36];
        Dictionary<int, string> originalValues = new Dictionary<int, string>();

        public void Add(int id, string value)
        {
            originalValues[id] = value;

            var words = value.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);

            var compressedTrigrams = new HashSet<ushort>();

            foreach (var word in words)
            {
                var normalizedWord = Utils.NormalizeString(word);
                Utils.AddCompressedTrigrams(normalizedWord, compressedTrigrams);
            }

            foreach (var compressedTrigram in compressedTrigrams)
            {
                if (trigramArr[compressedTrigram] == null)
                    trigramArr[compressedTrigram] = new LowMemorySet();

                trigramArr[compressedTrigram].Add(id);
                compressedTrigramCounts[compressedTrigram] = compressedTrigramCounts[compressedTrigram] + 1;
            }

            foreach(var ct in compressedTrigrams)
            {
                compressedTrigramCounts[ct] = trigramArr[ct].Count;
            }
        }

        public SearchResult Search(string searchTerm)
        {
            var startTime = DateTime.UtcNow;
            var words = searchTerm.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);
            var compressedTrigrams = new HashSet<ushort>();

            foreach (var word in words)
            {
                var normalizedWord = Utils.NormalizeString(word);
                Utils.AddCompressedTrigrams(normalizedWord, compressedTrigrams);
            }

            if (compressedTrigrams.Any())
                return SearchTrigrams(words, compressedTrigrams);

            return SearchScan(words);
        }

        private SearchResult SearchScan(string[] words)
        {
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

        private SearchResult SearchTrigrams(string[] words, HashSet<ushort> compressedTrigrams)
        {
            var startTime = DateTime.UtcNow;

            var searchTrigrams0 = ushort.MaxValue;
            var searchTrigrams1 = ushort.MaxValue;
            var searchTrigrams2 = ushort.MaxValue;

            foreach (var ct in compressedTrigrams)
            {
                int trigramCount = compressedTrigramCounts[ct];

                // if this trigram was not found in the index, ignore it.
                if (trigramCount == 0)
                    continue;

                else if (searchTrigrams0 == ushort.MaxValue)
                    searchTrigrams0 = ct;
                else if (searchTrigrams1 == ushort.MaxValue)
                    searchTrigrams1 = ct;
                else if (searchTrigrams2 == ushort.MaxValue)
                    searchTrigrams2 = ct;
                else
                {
                    if (trigramCount < compressedTrigramCounts[searchTrigrams0])
                    {
                        searchTrigrams2 = searchTrigrams1;
                        searchTrigrams1 = searchTrigrams0;
                        searchTrigrams0 = ct;
                    }
                    else if (trigramCount < compressedTrigramCounts[searchTrigrams1])
                    {
                        searchTrigrams2 = searchTrigrams1;
                        searchTrigrams1 = ct;
                    }
                    else if (trigramCount < compressedTrigramCounts[searchTrigrams2])
                    {
                        searchTrigrams2 = ct;
                    }
                }
            }

            // now find all IDs that contain the 3 trigrams with the fewest matches (i.e. the most selective)
            Dictionary<int, int> idsWithMatchCount = new Dictionary<int, int>();
            
            if (searchTrigrams0 != ushort.MaxValue)
            {
                //ids = new HashSet<int>(trigramArr[searchTrigrams0]);
                foreach (var id in trigramArr[searchTrigrams0])
                    idsWithMatchCount[id] = 1;

                if (searchTrigrams1 != ushort.MaxValue)
                {
                    //ids.IntersectWith(trigramArr[searchTrigrams1]);
                    foreach (var id in trigramArr[searchTrigrams1])
                    {
                        if (idsWithMatchCount.ContainsKey(id))
                            idsWithMatchCount[id] = 2;
                        else
                            idsWithMatchCount[id] = 1;
                    }

                    if (searchTrigrams2 != ushort.MaxValue)
                    {
                        foreach (var id in trigramArr[searchTrigrams1])
                        {
                            if (idsWithMatchCount.ContainsKey(id))
                                idsWithMatchCount[id] = idsWithMatchCount[id] + 1;
                            else
                                idsWithMatchCount[id] = 1;
                        }
                    }
                }
            }

            var searchResultExactMatchItems = new List<SearchResultItem>();
            var searchResultAlternativeMatchItems = new List<SearchResultItem>();

            var numberOfSearchTrigrams = 0;
            if (searchTrigrams0 != ushort.MaxValue)
            {
                numberOfSearchTrigrams += 1;
                if (searchTrigrams1 != ushort.MaxValue)
                {
                    numberOfSearchTrigrams += 1;
                    if (searchTrigrams2 != ushort.MaxValue)
                    {
                        numberOfSearchTrigrams += 1;
                    }
                }
            }

            foreach (var idWithMatchCount in idsWithMatchCount)
            {
                var id = idWithMatchCount.Key;
                var matchCount = idWithMatchCount.Value;

                // test whether this is an exact match (e.g. The search term 'aaaaaa' will otherwise return a match for the value 'aaa')
                // To be an exact match, each search word must be in the original value.
                var isExactMatch = true;

                if (matchCount < numberOfSearchTrigrams)
                    isExactMatch = false;

                if (isExactMatch)
                    foreach (var word in words)
                    {
                        // TODO: implement .Contains(string, StringComparison.OrdinalIgnoreCase))
                        if (!originalValues[id].Contains(word.ToLower()))
                        {
                            isExactMatch = false;
                            break;
                        }
                    }

                if (isExactMatch)
                    searchResultExactMatchItems.Add(new SearchResultItem { Id = id, OriginalValue = originalValues[id]});
                else
                    searchResultAlternativeMatchItems.Add(new SearchResultItem { Id = id, OriginalValue = originalValues[id]});
            }

            // Now rank the results in order of how many of the search term trigrams are contained in the result,
            // allowing 'did you mean' type results when there are no full matches.
            foreach (var searchResultItem in searchResultExactMatchItems)
            {
                var count = 0;
                foreach (var ct in compressedTrigrams)
                {
                    if (trigramArr[ct] == null)
                        continue;

                    if (trigramArr[ct].Contains(searchResultItem.Id))
                        count++;
                }
                searchResultItem.Score = (int)((float)count * 100 / compressedTrigrams.Count);
            }

            // Now rank the results in order of how many of the search term trigrams are contained in the result,
            // allowing 'did you mean' type results when there are no full matches.
            foreach (var searchResultItem in searchResultAlternativeMatchItems)
            { 
                var count = 0;
                foreach (var ct in compressedTrigrams)
                {
                    if (trigramArr[ct] == null)
                        continue;

                    if (trigramArr[ct].Contains(searchResultItem.Id))
                        count++;
                }
                searchResultItem.Score = (int)((float)count * 100 / compressedTrigrams.Count);
            }

            searchResultExactMatchItems.Sort((a, b) => -a.Score.CompareTo(b.Score));
            searchResultAlternativeMatchItems.Sort((a, b) => -a.Score.CompareTo(b.Score));

            // Only return 10 alternatives
            if (searchResultAlternativeMatchItems.Count > 10)
            {
                searchResultAlternativeMatchItems.RemoveRange(10, searchResultAlternativeMatchItems.Count - 10);
                searchResultAlternativeMatchItems.Capacity = 10;
            }

            for (int i = 0; i < searchResultExactMatchItems.Count; i++)
                searchResultExactMatchItems[i].Order = i;

            for (int i = 0; i < searchResultAlternativeMatchItems.Count; i++)
                searchResultAlternativeMatchItems[i].Order = i;

            var searchResult = new SearchResult 
            { 
                SearchResultExactMatchItems = searchResultExactMatchItems,
                SearchResultAlternativeMatchItems = searchResultAlternativeMatchItems,
                TotalSearchTime = DateTime.UtcNow.Subtract(startTime) 
            };

            return searchResult;
        }
    }
}
