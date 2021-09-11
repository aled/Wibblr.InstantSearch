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
            HashSet<int> ids = new HashSet<int>();
            var searchResultItems = new List<SearchResultItem>();

            if (searchTrigrams0 != ushort.MaxValue)
            {
                ids = new HashSet<int>(trigramArr[searchTrigrams0]);

                if (searchTrigrams1 != ushort.MaxValue)
                {
                    ids.IntersectWith(trigramArr[searchTrigrams1]);

                    if (searchTrigrams2 != ushort.MaxValue)
                    {
                        ids.IntersectWith(trigramArr[searchTrigrams2]);
                    }
                }
            }

            //searchResultItems.AddRange(ids.Select(x => new SearchResultItem { Id = x, OriginalValue = originalValues[x] }));
            foreach (var id in ids)
            {
                // test whether this is a valid result (e.g. The search term 'aaaaaa' will otherwise return a match for the value 'aaa')
                // each search word must be in the original value.
                var isValid = true;
                foreach (var word in words)
                {
                    if (!originalValues[id].Contains(word))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                    searchResultItems.Add(new SearchResultItem { Id = id, OriginalValue = originalValues[id] });
            }

            // Now rank the results in order of how many of the search term trigrams are contained in the result,
            // allowing 'did you mean' type results when there are no full matches.
            foreach (var searchResultItem in searchResultItems)
            {
                var count = 0;
                foreach(var ct in compressedTrigrams)
                {
                    if (trigramArr[ct] == null)
                        continue;

                    if (trigramArr[ct].Contains(searchResultItem.Id))
                        count++;
                }
                searchResultItem.Score = (int)((float)count * 100 / compressedTrigrams.Count);
            }

            searchResultItems.Sort((a, b) => -a.Score.CompareTo(b.Score));

            for (int i = 0; i < searchResultItems.Count; i++)
            {
                searchResultItems[i].Order = i;
            }

            var searchResult = new SearchResult 
            { 
                SearchResultItems = searchResultItems, 
                TotalSearchTime = DateTime.UtcNow.Subtract(startTime) 
            };

            return searchResult;
        }
    }
}
