namespace SOFT_DB_EXAM.Entities;

public class WatchListsFollowed
{
    public int Id { get; set; } // optional, or can use composite key
    public int UserId { get; set; }
    public int WatchListId { get; set; }

    public User User { get; set; }
    public WatchList WatchList { get; set; }
    
    public WatchListsFollowed()
    {
    }
}
