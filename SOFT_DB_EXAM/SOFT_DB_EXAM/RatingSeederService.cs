using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SOFT_DB_EXAM;

public class RatingSeederService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMongoCollection<Movie> _movies;
    private readonly ILogger<RatingSeederService> _logger;

    public RatingSeederService(ApplicationDbContext dbContext, IMongoClient mongoClient, IOptions<MongoDbSettings> mongoSettings, ILogger<RatingSeederService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        var db = mongoClient.GetDatabase(mongoSettings.Value.DatabaseName);
        _movies = db.GetCollection<Movie>(mongoSettings.Value.CollectionName);
    }

    public async Task SeedAverageRatingsIfEmptyAsync()
    {
        var existingCount = await _dbContext.AverageRatings.CountAsync();

        if (existingCount > 0)
        {
            _logger.LogInformation("AverageRatings already initialized with {Count} entries. Skipping seeding.", existingCount);
            return;
        }

        _logger.LogInformation("Seeding AverageRatings from MongoDB movies...");

        var movies = await _movies.Find(_ => true).ToListAsync();

        var averageRatings = movies.Select(m => new AverageRating
        {
            MovieId = m.MovieId,
            AverageRatings = (decimal) m.Vote_Average,
            NumberOfRatings = m.Vote_Count,
        }).ToList();

        _dbContext.AverageRatings.AddRange(averageRatings);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} AverageRating entries.", averageRatings.Count);
    }
}