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
    public class SearchIndexTrigramSet : AbstractSearchIndex
    {
        int[] trigramCounts = new int[36 * 36 * 36];

        ISet<int>[] trigramArr = new LowMemorySet[36 * 36 * 36];
      
        public void Add(int id, string value)
        {
            originalValues[id] = value;

            var words = value.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);

            var trigrams = new HashSet<Trigram>();

            foreach (var word in words)
            {
                var normalizedWord = Utils.NormalizeString(word);
                Utils.AddTrigrams(normalizedWord, trigrams);
            }

            foreach (var t in trigrams)
            {
                if (trigramArr[t.Ordinal] == null)
                    trigramArr[t.Ordinal] = new LowMemorySet();

                trigramArr[t.Ordinal].Add(id);
                trigramCounts[t.Ordinal] = trigramCounts[t.Ordinal] + 1;
            }

            foreach(var t in trigrams)
            {
                trigramCounts[t.Ordinal] = trigramArr[t.Ordinal].Count;
            }
        }

        public override SearchResult Search(string searchTerm)
        {
            var startTime = DateTime.UtcNow;
            var words = searchTerm.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);
            var trigrams = new HashSet<Trigram>();

            foreach (var word in words)
            {
                var normalizedWord = Utils.NormalizeString(word);
                Utils.AddTrigrams(normalizedWord, trigrams);
            }

            if (trigrams.Any())
                return SearchTrigrams(words, trigrams);

            else 
                return Scan(searchTerm);
        }

        private SearchResult SearchTrigrams(string[] words, HashSet<Trigram> compressedTrigrams)
        {
            var startTime = DateTime.UtcNow;

            var t0 = Trigram.Invalid;
            var t1 = Trigram.Invalid;
            var t2 = Trigram.Invalid;

            foreach (var ct in compressedTrigrams)
            {
                int trigramCount = trigramCounts[ct.Ordinal];

                // if this trigram was not found in the index, ignore it.
                if (trigramCount == 0)
                    continue;

                else if (t0.Ordinal == Trigram.Invalid.Ordinal)
                    t0 = ct;
                else if (t1.Ordinal == Trigram.Invalid.Ordinal)
                    t1 = ct;
                else if (t2.Ordinal == Trigram.Invalid.Ordinal)
                    t2 = ct;
                else
                {
                    if (trigramCount < trigramCounts[t0.Ordinal])
                    {
                        t2 = t1;
                        t1 = t0;
                        t0 = ct;
                    }
                    else if (trigramCount < trigramCounts[t1.Ordinal])
                    {
                        t2 = t1;
                        t1 = ct;
                    }
                    else if (trigramCount < trigramCounts[t2.Ordinal])
                    {
                        t2 = ct;
                    }
                }
            }

            // now find all IDs that contain the 3 trigrams with the fewest matches (i.e. the most selective)
            Dictionary<int, int> idsWithMatchCount = new Dictionary<int, int>();
            
            if (t0.Ordinal != ushort.MaxValue)
            {
                //ids = new HashSet<int>(trigramArr[searchTrigrams0]);
                foreach (var id in trigramArr[t0.Ordinal])
                    idsWithMatchCount[id] = 1;

                if (t1.Ordinal != ushort.MaxValue)
                {
                    //ids.IntersectWith(trigramArr[searchTrigrams1]);
                    foreach (var id in trigramArr[t1.Ordinal])
                    {
                        if (idsWithMatchCount.ContainsKey(id))
                            idsWithMatchCount[id] = 2;
                        else
                            idsWithMatchCount[id] = 1;
                    }

                    if (t2.Ordinal != ushort.MaxValue)
                    {
                        foreach (var id in trigramArr[t1.Ordinal])
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
            if (t0.Ordinal != ushort.MaxValue)
            {
                numberOfSearchTrigrams += 1;
                if (t1.Ordinal != ushort.MaxValue)
                {
                    numberOfSearchTrigrams += 1;
                    if (t2.Ordinal != ushort.MaxValue)
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
                    if (trigramArr[ct.Ordinal] == null)
                        continue;

                    if (trigramArr[ct.Ordinal].Contains(searchResultItem.Id))
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
                    if (trigramArr[ct.Ordinal] == null)
                        continue;

                    if (trigramArr[ct.Ordinal].Contains(searchResultItem.Id))
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
