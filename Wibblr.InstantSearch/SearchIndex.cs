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

        Dictionary<ushort, HashSet<int>> trigramDict = new Dictionary<ushort, HashSet<int>>();
        Dictionary<int, string> originalValues = new Dictionary<int, string>();

        public void Add(int id, string value)
        {
            originalValues[id] = value;

            var words = value.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);

            var compressedTrigrams = new HashSet<ushort>();

            foreach (var word in words)
            {
                var normalizedWord = Utils.NormalizeString(word);

                //var trigrams = Utils.GenerateSubstrings(normalizedWord, 3);
                Utils.AddCompressedTrigrams(normalizedWord, compressedTrigrams);
            }

            foreach (var compressedTrigram in compressedTrigrams)
            {
                //var compressedTrigram = Utils.CompressTrigram(trigram);

                if (!trigramDict.ContainsKey(compressedTrigram))
                    trigramDict[compressedTrigram] = new HashSet<int>();

                trigramDict[compressedTrigram].Add(id);
                compressedTrigramCounts[compressedTrigram] = compressedTrigramCounts[compressedTrigram] + 1;
                //Console.WriteLine($"added id {id} for trigram {trigram.ToString()} ({compressedTrigram.ToString()})");
            }

            foreach(var ct in compressedTrigrams)
            {
                compressedTrigramCounts[ct] = trigramDict[ct].Count;
            }
        }

        public SearchResult Search(string searchTerm)
        {
            var start = DateTime.UtcNow;
            var splits = new List<TimeSpan>();

            var words = searchTerm.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);

           
            //var trigrams = new HashSet<Slice>();
            var compressedTrigrams = new HashSet<ushort>();
            var timeSplit1 = DateTime.UtcNow;
            var timeSplit2 = DateTime.UtcNow; 
            
            foreach (var word in words)
            {
                var normalizedWord = Utils.NormalizeString(word);
                //var normalizedWord = Encoding.ASCII.GetBytes(word);
                timeSplit2 = DateTime.UtcNow;
                Utils.AddCompressedTrigrams(normalizedWord, compressedTrigrams);
            }

            //var compressedTrigrams = trigrams
            //   .Select(x => Utils.CompressTrigram(x))
            //    .ToArray();

            var timeSplit3 = DateTime.UtcNow;


            // Get 3 most selective trigrams, i.e. those with the fewest number of matches

            // This takes over 3 milliseconds, so rewrite below.
            //var searchTrigrams = compressedTrigrams
            //    .Where(ct => trigramDict.ContainsKey(ct))
            //    .Select(ct => new { ct, count = trigramDict[ct].Count })
            //    .OrderBy(x => x.count)
            //    .Select(x => x.ct)
            //    .ToArray();

            // This replicates the above statement, but is faster.
            var searchTrigrams = new List<ushort>();

            foreach (var ct in compressedTrigrams)
            {
                int trigramCount = compressedTrigramCounts[ct];

                if (trigramCount > 0)
                {
                    if (searchTrigrams.Count < 3)
                    {
                        searchTrigrams.Add(ct);
                    }
                    else if (trigramCount < compressedTrigramCounts[searchTrigrams[0]])
                    {
                        searchTrigrams[2] = searchTrigrams[1];
                        searchTrigrams[1] = searchTrigrams[0];
                        searchTrigrams[0] = ct;
                    }
                    else if (trigramCount < compressedTrigramCounts[searchTrigrams[1]])
                    {
                        searchTrigrams[2] = searchTrigrams[1];
                        searchTrigrams[1] = ct;
                    }
                    else if (trigramCount< compressedTrigramCounts[searchTrigrams[2]])
                    {
                        searchTrigrams[2] = ct;
                    }
                }
            }

            var timeSplit4 = DateTime.UtcNow;


            // now find all IDs that contain the 3 trigrams with the fewest matches (i.e. the most selective)
            HashSet<int> ids = new HashSet<int>();
            var searchResultItems = new List<SearchResultItem>();

            var timeSplit5 = DateTime.UtcNow;
            var timeSplit6 = DateTime.UtcNow;
            var timeSplit7 = DateTime.UtcNow;

            if (searchTrigrams.Count >= 1)
            {
                ids = trigramDict[searchTrigrams[0]];
                timeSplit5 = DateTime.UtcNow;
                timeSplit6 = DateTime.UtcNow;
                timeSplit7 = DateTime.UtcNow;
                if (searchTrigrams.Count >= 2)
                {
                    ids.IntersectWith(trigramDict[searchTrigrams[1]]);
                    timeSplit6 = DateTime.UtcNow;
                    timeSplit7 = DateTime.UtcNow;

                    if (searchTrigrams.Count >= 3)
                    {
                        ids.IntersectWith(trigramDict[searchTrigrams[2]]);

                        timeSplit7 = DateTime.UtcNow;
                    }
                }
            }


            //searchResultItems.AddRange(ids.Select(x => new SearchResultItem { Id = x, OriginalValue = originalValues[x] }));
            foreach (var id in ids)
            {
                // test whether this is a valid result (e.g. The search term 'aaaaaa' will otherwise return a match for the value 'aaa')
                if (originalValues[id].Contains(searchTerm))
                    searchResultItems.Add(new SearchResultItem { Id = id, OriginalValue = originalValues[id] });
            }


            var timeSplit8 = DateTime.UtcNow;

            // Now rank the results in order of how many of the search term trigrams are contained in the result,
            // allowing 'did you mean' type results when there are no full matches.
            foreach (var searchResultItem in searchResultItems)
            {
                var count = 0;
                foreach(var ct in compressedTrigrams)
                {
                    if (!trigramDict.ContainsKey(ct))
                        continue;

                    if (trigramDict[ct].Contains(searchResultItem.Id))
                        count++;
                }
                searchResultItem.Score = (int)((float)count * 100 / compressedTrigrams.Count);
            }


            var timeSplit9 = DateTime.UtcNow;

            searchResultItems.Sort((a, b) => -a.Score.CompareTo(b.Score));

            var timeSplit10 = DateTime.UtcNow;

            for (int i = 0; i < searchResultItems.Count; i++)
            {
                searchResultItems[i].Order = i;
            }
            var timeSplit11 = DateTime.UtcNow;

            var searchResult = new SearchResult 
            { 
                Splits = new TimeSpan[] {
                    timeSplit1.Subtract(start),
                    timeSplit2.Subtract(timeSplit1),
                    timeSplit3.Subtract(timeSplit2),
                    timeSplit4.Subtract(timeSplit3),
                    timeSplit5.Subtract(timeSplit4),
                    timeSplit6.Subtract(timeSplit5),
                    timeSplit7.Subtract(timeSplit6),
                    timeSplit8.Subtract(timeSplit7),
                    timeSplit9.Subtract(timeSplit8),
                    timeSplit10.Subtract(timeSplit9),
                    timeSplit11.Subtract(timeSplit10),
                },
                SearchResultItems = searchResultItems, 
                TotalSearchTime = DateTime.UtcNow.Subtract(start) 
            };

            return searchResult;
        }
    }

    public struct Slice
    {
        public byte[] ascii;
        public short start;
        public short len;

        public Slice(byte[] ascii, short start, short len) : this()
        {
            this.ascii = ascii;
            this.start = start;
            this.len = len;
        }

        public override string ToString()
        {
            return Encoding.ASCII.GetString(ascii, start, len);
        }
    }
}
