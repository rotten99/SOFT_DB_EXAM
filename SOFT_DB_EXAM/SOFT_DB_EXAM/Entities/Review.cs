using System.ComponentModel.DataAnnotations.Schema;

public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MovieId { get; set; } // MongoDB reference

    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Rating { get; set; }
    
    [NotMapped]
    public Movie Movie { get; set; } 
    
    public Review()
    {
    }
}