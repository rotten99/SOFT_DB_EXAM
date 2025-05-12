public class WatchList
{
    public int Id { get; set; }
    public DateTime AddedDate { get; set; }
    public string Name { get; set; }
    public bool IsPrivate { get; set; }
    public ICollection<ListedMovie> ListedMovies { get; set; }
    
    public WatchList()
    {
        
    }
}