using MongoDB.Bson.Serialization.Attributes;

[BsonIgnoreExtraElements]
public class Movie
{
    [BsonElement("id")]
    public int MovieId { get; set; }


    [BsonElement("title")]
    public string Title { get; set; }

    [BsonElement("vote_average")]
    public double Vote_Average { get; set; }

    [BsonElement("vote_count")]
    public int Vote_Count { get; set; }

    [BsonElement("status")]
    public string Status { get; set; }

    [BsonElement("release_date")]
    public DateTime Release_Date { get; set; }

    [BsonElement("revenue")]
    public long Revenue { get; set; }

    [BsonElement("runtime")]
    public int Runtime { get; set; }

    [BsonElement("adult")]
    public bool Adult { get; set; }

    [BsonElement("backdrop_path")]
    public string Backdrop_Path { get; set; }

    [BsonElement("budget")]
    public long Budget { get; set; }

    [BsonElement("homepage")]
    public string Homepage { get; set; }

    [BsonElement("imdb_id")]
    public string Imdb_Id { get; set; }

    [BsonElement("original_language")]
    public string Original_Language { get; set; }

    [BsonElement("original_title")]
    public string Original_Title { get; set; }

    [BsonElement("overview")]
    public string Overview { get; set; }

    [BsonElement("popularity")]
    public double Popularity { get; set; }

    [BsonElement("poster_path")]
    public string Poster_Path { get; set; }

    [BsonElement("tagline")]
    public string Tagline { get; set; }

    [BsonElement("genres")]
    public string Genres { get; set; }

    [BsonElement("production_companies")]
    public string Production_Companies { get; set; }

    [BsonElement("production_countries")]
    public string Production_Countries { get; set; }

    [BsonElement("spoken_languages")]
    public string Spoken_Languages { get; set; }

    [BsonElement("keywords")]
    public string Keywords { get; set; }
}