﻿using System;
using System.Collections.Generic;

namespace Wibblr.InstantSearch
{
    public class SearchResult
    {
        public TimeSpan TotalSearchTime;
        public IList<SearchResultItem> SearchResultItems;
    }
}
