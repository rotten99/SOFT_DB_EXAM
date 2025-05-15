using System.ComponentModel.DataAnnotations.Schema;

public class FavouriteMovie
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MovieId { get; set; } // MongoDB reference
    [NotMapped]
    public Movie Movie { get; set; } // Navigation property for the Movie entity

    public FavouriteMovie()
    {
    }
}