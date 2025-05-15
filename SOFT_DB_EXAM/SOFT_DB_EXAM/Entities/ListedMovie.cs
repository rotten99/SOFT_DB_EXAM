using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class ListedMovie
{
    public int Id { get; set; }
    [JsonIgnore]
    public ICollection<WatchList> WatchLists { get; set; }
    public int MovieId { get; set; } // MongoDB reference
    public bool HasWatched { get; set; }
    
    [NotMapped]
    public Movie Movie { get; set; } // Navigation property for the Movie entity
    
    public ListedMovie()
    {
    }
}