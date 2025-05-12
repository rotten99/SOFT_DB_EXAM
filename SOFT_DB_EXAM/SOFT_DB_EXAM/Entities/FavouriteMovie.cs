public class FavouriteMovie
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MovieId { get; set; } // MongoDB reference

    public FavouriteMovie()
    {
    }
}