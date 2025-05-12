
using SOFT_DB_EXAM.Entities;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MoviesReviewed { get; set; }
    public int MoviesWatched { get; set; }

    public ICollection<Review> Reviews { get; set; }
    public ICollection<WatchList> WatchListsOwned { get; set; }
    public ICollection<WatchListsFollowed> WatchListsFollowed { get; set; }
    public ICollection<FavouriteMovie> FavouriteMovies { get; set; }

    public User()
    {
        
    }
}
