using SOFT_DB_EXAM.Entities;

public class WatchList
{
    public int Id { get; set; }
    public DateTime AddedDate { get; set; }
    public string Name { get; set; }
    public bool IsPrivate { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public ICollection<ListedMovie> ListedMovies { get; set; }
    public ICollection<WatchListsFollowed> Followers { get; set; }
    
    public WatchList()
    {
        
    }
}