namespace SOFT_DB_EXAM.Dtos;


public class CreateReviewDto
{
    public string ReviewText { get; set; } = null!;
    public int Rating { get; set; }
    public int MovieId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
}
