namespace SOFT_DB_EXAM.Entities;

public class User
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MoviesReviewed { get; set; }
    public int MoviesWatched { get; set; }
    public List<WatchList> WatchLists { get; set; }
    public List<Review> Reviews { get; set; }

    public User()
    {
    }
}