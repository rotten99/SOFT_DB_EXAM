namespace SOFT_DB_EXAM.Dtos;

public class CreateWatchPartyDto
{
    public string Title { get; set; }
    public List<int> MovieIds { get; set; }
    public List<int> UserIds { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
