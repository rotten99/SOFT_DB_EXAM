namespace SOFT_DB_EXAM.Entities;

public class Review
{
    public int MovieId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Rating { get; set; }

    public Review()
    {
    }
}