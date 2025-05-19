using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SOFT_DB_EXAM.Entities;

namespace SOFT_DB_EXAM.Facades;

public class MovieFacade
{
    private readonly IMongoCollection<Movie> _movies;
    private readonly ILogger<MovieFacade> _logger;
    private readonly RedisFacade _redis;
    private const int CacheTtlSeconds = 300; // 5 minutes

    public MovieFacade(IOptions<MongoDbSettings> settings, IMongoClient mongoClient, ILogger<MovieFacade> logger, RedisFacade redis)
    {
        _logger = logger;
        _redis = redis;
        var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
        _movies = database.GetCollection<Movie>(settings.Value.CollectionName);
        _logger.LogInformation("MovieFacade initialized with DB: {Database} and Collection: {Collection}",
            settings.Value.DatabaseName, settings.Value.CollectionName);
    }
    
    public async Task UpdateRatingAsync(int movieId, double average, int count)
    {
        var update = Builders<Movie>.Update
            .Set(m => m.Vote_Average, average)
            .Set(m => m.Vote_Count, count);

        await _movies.UpdateOneAsync(m => m.MovieId == movieId, update);

        _logger.LogInformation("Updated movie {MovieId} with new rating {Rating} and count {Count}", movieId, average, count);

        // Invalidate cached movie
        var cacheKey = $"movie:{movieId}";
        await _redis.SetStringAsync(cacheKey,
            System.Text.Json.JsonSerializer.Serialize(await GetByIdAsync(movieId)),
            TimeSpan.FromSeconds(CacheTtlSeconds));
    }



    public async Task<Movie> GetByIdAsync(int id)
    {
        _logger.LogInformation("Fetching movie with ID: {MovieId}", id);

        var cacheKey = $"movie:{id}";
        var cachedMovieJson = await _redis.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedMovieJson))
        {
            _logger.LogInformation("Movie with ID {MovieId} found in cache.", id);
            return System.Text.Json.JsonSerializer.Deserialize<Movie>(cachedMovieJson)!;
        }

        var filter = Builders<Movie>.Filter.Eq(m => m.MovieId, id);
        var result = await _movies.Find(filter).FirstOrDefaultAsync();

        if (result == null)
        {
            _logger.LogWarning("Movie with ID {MovieId} not found in MongoDB.", id);
        }
        else
        {
            _logger.LogInformation("Caching movie with ID {MovieId}.", id);
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            await _redis.SetStringAsync(cacheKey, json, TimeSpan.FromSeconds(CacheTtlSeconds));
        }

        return result;
    }



    public async Task<List<Movie>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("Fetching all movies: Page {Page}, PageSize {PageSize}", page, pageSize);
        var movies = await _movies.Find(m => m.Vote_Count > 1000)
            .SortByDescending(m => m.Vote_Average)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
        _logger.LogInformation("Fetched {Count} movies.", movies.Count);
        return movies;
    }
    
    public async Task<List<Movie>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var result = new List<Movie>();
        var idsToFetchFromMongo = new List<int>();

        foreach (var id in ids)
        {
            var cacheKey = $"movie:{id}";
            var cachedMovieJson = await _redis.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedMovieJson))
            {
                _logger.LogInformation("Movie {Id} loaded from cache.", id);
                result.Add(System.Text.Json.JsonSerializer.Deserialize<Movie>(cachedMovieJson)!);
            }
            else
            {
                _logger.LogInformation("Movie {Id} not found in cache, will query MongoDB.", id);
                idsToFetchFromMongo.Add(id);
            }
        }

        if (idsToFetchFromMongo.Any())
        {
            var filter = Builders<Movie>.Filter.In(m => m.MovieId, idsToFetchFromMongo);
            var mongoMovies = await _movies.Find(filter).ToListAsync();

            foreach (var movie in mongoMovies)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(movie);
                await _redis.SetStringAsync($"movie:{movie.MovieId}", json, TimeSpan.FromMinutes(5));
                result.Add(movie);
            }
        }

        return result;
    }


    public async Task<List<Movie>> SearchByTitleAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("Searching movies with term: \"{SearchTerm}\"", searchTerm);
        var filter = Builders<Movie>.Filter.Regex(m => m.Title, new BsonRegularExpression(searchTerm, "i"));
        var result = await _movies.Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        _logger.LogInformation("Search returned {Count} movies.", result.Count);
        return result;
    }
    
    public async Task<List<Movie>> SearchByKeywordsAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("Performing multi-field search with query: \"{SearchTerm}\"", searchTerm);

        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<Movie>();

        var words = searchTerm
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Build OR conditions for all words across all relevant fields
        var orFilters = new List<FilterDefinition<Movie>>();

        foreach (var word in words)
        {
            var regex = new BsonRegularExpression(word, "i"); // case-insensitive partial match
            orFilters.Add(Builders<Movie>.Filter.Regex(m => m.Title, regex));
            orFilters.Add(Builders<Movie>.Filter.Regex(m => m.Overview, regex));
            orFilters.Add(Builders<Movie>.Filter.Regex(m => m.Genres, regex));
            orFilters.Add(Builders<Movie>.Filter.Regex(m => m.Keywords, regex));
        }

        var finalFilter = Builders<Movie>.Filter.Or(orFilters);

        var result = await _movies.Find(finalFilter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        _logger.LogInformation("Search with term \"{SearchTerm}\" returned {Count} results.", searchTerm, result.Count);
        return result;
    }

}