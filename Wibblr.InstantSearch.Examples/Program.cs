using System;
using System.Linq;
using System.Text;

namespace Wibblr.InstantSearch.Examples
{
    class Program
    {
        SearchIndex index = new SearchIndex();

        private string RandomString(int len)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                int r = Random.Shared.Next() % 36;

                if (r < 10) 
                    sb.Append((char)('0' + r));
                else 
                    sb.Append((char)('a' + r - 10));
            }
            return sb.ToString();
        }

        static void Main(string[] args)
        {
            new Program().Run();
        }

        void Run()
        {
            int numStrings = 5_000_000;
            Console.WriteLine($"Adding {numStrings} strings");
            for (int i = 0; i < numStrings; i++)
            {
                index.Add(i, RandomString(15));
            }

            Console.WriteLine("Search-as-you-type!");
            string searchTerm = "";
            while (true)
            {
                var c = Console.ReadKey(true).KeyChar;

                if (c == '\r')
                {
                    searchTerm = "";
                    continue;
                }

                searchTerm += c;

                var result = index.Search(searchTerm);

                Console.WriteLine("search: " + searchTerm + "; " + result.SearchResultItems.Count + " results in " + result.TotalSearchTime.TotalMilliseconds.ToString() + "ms");
                Console.WriteLine(string.Join("\n", result.SearchResultItems.Select(x => x.ToString())));
            }
        }
    }
}
