using System;
using System.Collections.Generic;
using System.Text;

namespace Wibblr.InstantSearch
{
    /// <summary>
    /// Search engine contains many indexes
    /// </summary>
    public class SearchEngine
    {
        Dictionary<string, AbstractSearchIndex> indexes = new Dictionary<string, AbstractSearchIndex>();

        public bool CreateIndex(string name, string type = "trigramset")
        {
            if (indexes.ContainsKey(name))
                return false;

            indexes[name] = new SearchIndexTrigramSet();

            return true;
        }

        
    }
}
