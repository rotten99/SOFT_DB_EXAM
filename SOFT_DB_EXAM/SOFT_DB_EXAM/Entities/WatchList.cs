namespace SOFT_DB_EXAM.Entities;

public class WatchList
{
    public int MovieId { get; set; }
    public DateTime AddedAt { get; set; }
    public bool HasWatched { get; set; }

    public WatchList()
    {
    }
}