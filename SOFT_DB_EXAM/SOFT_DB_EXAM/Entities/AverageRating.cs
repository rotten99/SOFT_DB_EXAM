public class AverageRating
{
    public int Id { get; set; }
    public int MovieId { get; set; } // MongoDB reference
    public decimal AverageRatings { get; set; }
    public int NumberOfRatings { get; set; }

    public AverageRating()
    {
    }
}