using System.ComponentModel.DataAnnotations.Schema;

public class AverageRating
{
    public int Id { get; set; }
    public int MovieId { get; set; } // MongoDB reference
    public decimal AverageRatings { get; set; }
    public int NumberOfRatings { get; set; }
    
    [NotMapped]
    public Movie Movie { get; set; } // Navigation property for the Movie entity

    public AverageRating()
    {
    }
}