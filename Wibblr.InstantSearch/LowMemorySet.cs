using System.Collections;

namespace Wibblr.InstantSearch
{
    class LowMemorySet : ISet<int>
    {
        private int _maxUnsortedItems = 1024;

        public int MaxUnsortedItems 
        { 
            get => _maxUnsortedItems;
            set
            {
                _maxUnsortedItems = value;
                
                if (uniqueUnsortedItems.Count > _maxUnsortedItems)
                    Optimize();

                uniqueUnsortedItems.Capacity = _maxUnsortedItems;
            }
        }

        public List<int> uniqueSortedItems = new List<int>();
        private List<int> uniqueUnsortedItems = new List<int>(); // stuff only goes in here if not already in uniqueSortedItems

        public int Count => uniqueSortedItems.Count + uniqueUnsortedItems.Count;

        public bool IsReadOnly => false;

        public void Optimize()
        {
            // can optimize(!) this process by copying only the things that need moving.
            if (uniqueUnsortedItems.Count == 0)
                return;

            uniqueSortedItems.EnsureCapacity(uniqueSortedItems.Count + uniqueUnsortedItems.Count + _maxUnsortedItems);
            uniqueSortedItems.AddRange(uniqueUnsortedItems);
            uniqueSortedItems.Sort();
            uniqueUnsortedItems.Clear();
        }

        public bool Add(int item)
        {
            int index = uniqueSortedItems.BinarySearch(item);
            
            if (index >= 0)
                return false;

            uniqueUnsortedItems.Add(item);

            if (uniqueUnsortedItems.Count >= _maxUnsortedItems)
                Optimize();

            return true;
        }

        public void Clear()
        {
            uniqueSortedItems.Clear();
            uniqueUnsortedItems.Clear();
        }

        public bool Contains(int item)
        {
            if (uniqueSortedItems.BinarySearch(item) >= 0)
                return true;

            if (uniqueUnsortedItems.Contains(item))
                return true;

            return false;
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public void ExceptWith(IEnumerable<int> other)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<int> GetEnumerator()
        {
            Optimize();
            return uniqueSortedItems.GetEnumerator();
        }

        public void IntersectWith(IEnumerable<int> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSubsetOf(IEnumerable<int> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSupersetOf(IEnumerable<int> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSubsetOf(IEnumerable<int> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSupersetOf(IEnumerable<int> other)
        {
            throw new NotImplementedException();
        }

        public bool Overlaps(IEnumerable<int> other)
        {
            throw new NotImplementedException();
        }

        public bool Remove(int item)
        {
            throw new NotImplementedException();
        }

        public bool SetEquals(IEnumerable<int> other)
        {
            throw new NotImplementedException();
        }

        public void SymmetricExceptWith(IEnumerable<int> other)
        {
            throw new NotImplementedException();
        }

        public void UnionWith(IEnumerable<int> other)
        {
            throw new NotImplementedException();
        }

        void ICollection<int>.Add(int item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        
    }
}
