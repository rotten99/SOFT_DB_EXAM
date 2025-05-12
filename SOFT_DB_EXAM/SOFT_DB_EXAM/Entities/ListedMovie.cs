public class ListedMovie
{
    public int Id { get; set; }
    public ICollection<WatchList> WatchLists { get; set; }
    public int MovieId { get; set; } // MongoDB reference
    public bool HasWatched { get; set; }
    
    public ListedMovie()
    {
    }
}