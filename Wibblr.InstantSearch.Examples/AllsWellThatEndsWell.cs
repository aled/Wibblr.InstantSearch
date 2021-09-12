using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Wibblr.InstantSearch.Examples
{
    class AllsWellThatEndsWell
    {
        static void Main(string[] args)
        {
            var searchIndex = new SearchIndex();

            var a = Assembly.GetExecutingAssembly();
            var textName = a.GetManifestResourceNames().Single(x => x.EndsWith("AllsWellThatEndsWell.txt"));
            var textStream = a.GetManifestResourceStream(textName);

            Console.WriteLine("loading text");
            int lineNumber = 0;
            using (var r = new StreamReader(textStream))
            {
                var line = r.ReadLine();
                while (line != null)
                {
                    searchIndex.Add(lineNumber, line);
                    line = r.ReadLine();
                    lineNumber++;
                }
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

                var result = searchIndex.Search(searchTerm);

                Console.WriteLine("\r\nSearch complete: '" + searchTerm + "'; " + result.SearchResultExactMatchItems.Count + " exact results " + result.SearchResultAlternativeMatchItems.Count + " non-exact results in " + result.TotalSearchTime.TotalMilliseconds.ToString() + "ms");
                Console.WriteLine(string.Join("\r\n", result.SearchResultExactMatchItems.Select(x => x.ToString())));
                Console.WriteLine(string.Join("\r\n", result.SearchResultAlternativeMatchItems.Select(x => x.ToString())));
            }
        }
    }
}
