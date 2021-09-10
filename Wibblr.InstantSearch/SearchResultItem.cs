namespace Wibblr.InstantSearch
{

    public class SearchResultItem
    {
        public int Order { get; set; }
        public int Score { get; set; }
        public int Id { get; set; }
        public string OriginalValue { get; set; }

        public override string ToString()
        {
            return $"{Order}: '{OriginalValue}' (id {Id}, score {Score})";
        }
    }
}
