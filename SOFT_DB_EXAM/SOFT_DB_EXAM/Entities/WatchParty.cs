using System.ComponentModel.DataAnnotations.Schema;
using SOFT_DB_EXAM.Facades;

namespace SOFT_DB_EXAM.Entities;

public class WatchParty
{
    public int Id { get; set; }
    public string Title { get; set; }
    public List<int> MovieIds { get; set; } // MongoDB refs
    public List<int> UserIds { get; set; } // Participants
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [NotMapped]
    public List<Movie> Movies { get; set; }

    [NotMapped]
    public List<User> Users { get; set; }
    
    
}
