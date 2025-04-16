namespace SOFT_DB_EXAM.Entities;

public class WatchList
{
    public List<int> MovieIds { get; set; }
    public DateTime AddedAt { get; set; }
    public bool HasWatched { get; set; }

    public WatchList()
    {
    }
}