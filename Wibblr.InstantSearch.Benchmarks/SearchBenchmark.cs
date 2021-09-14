using System;
using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Wibblr.InstantSearch.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net472, launchCount: 1, warmupCount: 1, targetCount: 1)]
    [SimpleJob(RuntimeMoniker.Net50, launchCount: 1, warmupCount: 1, targetCount: 1)]
    [SimpleJob(RuntimeMoniker.Net60, launchCount: 1, warmupCount: 1, targetCount: 1)]
    public class SearchBenchmark
    {
        private readonly SearchIndexTrigramSet searchIndex = new SearchIndexTrigramSet();
        private readonly Random random = new Random();

        private string RandomString(int len)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                int r = random.Next() % 36;

                if (r < 10) sb.Append((char)('0' + r));
                else sb.Append((char)('a' + r - 10));
            }
            return sb.ToString();
        }

        public SearchBenchmark()
        {
            for (int i = 0; i < 100000; i++)
            {
                searchIndex.Add(i, RandomString(10));
            }
        }

        [Benchmark]
        public void Search3Letters()
        {
            var result = searchIndex.Search("poi");
        }

        [Benchmark]
        public void Search4Letters()
        {
            var result = searchIndex.Search("asdf");
            //Console.WriteLine(result.TotalSearchTime.TotalMilliseconds.ToString() + "ms");
            //Console.WriteLine(string.Join("\n", result.SearchResultItems.Select(x => x.ToString())));

            //Console.WriteLine(string.Join("\n", result.Splits.Select(x => x.TotalMilliseconds.ToString() + "ms")));
        }

        [Benchmark]
        public void Search5Letters()
        {
            var result = searchIndex.Search("werty");
        } 
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<SearchBenchmark>();
        }
    }
}
