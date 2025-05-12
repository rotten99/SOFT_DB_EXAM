namespace SOFT_DB_EXAM.Entities;

public class FollowedWatchList
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public ICollection<WatchList> WatchLists { get; set; }

    public FollowedWatchList()
    {
    }
}