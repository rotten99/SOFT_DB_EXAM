using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOFT_DB_EXAM.Entities;

namespace SOFT_DB_EXAM.Facades;

public class FavouriteMovieFacade
{
    private readonly ILogger<FavouriteMovieFacade> _logger;
    private readonly RedisFacade _redis;
    private readonly MovieFacade _movieFacade;
    private const int CacheTtlSeconds = 300; // 5 min TTL

    public FavouriteMovieFacade(ILogger<FavouriteMovieFacade> logger, RedisFacade redis, MovieFacade movieFacade)
    {
        _logger = logger;
        _redis = redis;
        _movieFacade = movieFacade;
    }

    public async Task AddFavouriteAsync(int userId, int movieId)
    {
        using var context = ApplicationContextFactory.CreateDbContext();

        bool alreadyExists = context.FavouriteMovies.Any(fm => fm.UserId == userId && fm.MovieId == movieId);
        if (alreadyExists)
        {
            _logger.LogInformation("Movie {MovieId} already favorited by user {UserId}", movieId, userId);
            return;
        }

        var fav = new FavouriteMovie
        {
            UserId = userId,
            MovieId = movieId
        };

        context.FavouriteMovies.Add(fav);
        context.SaveChanges();

        _logger.LogInformation("Added movie {MovieId} to favourites for user {UserId}", movieId, userId);

        // Invalidate cache
        var cacheKey = $"favourites:user:{userId}";
        await _redis.DeleteKeyAsync(cacheKey);
        _logger.LogInformation("Cleared Redis cache for user {UserId} favourites", userId);
    }

    public async Task RemoveFavouriteAsync(int userId, int movieId)
    {
        using var context = ApplicationContextFactory.CreateDbContext();

        var entry = context.FavouriteMovies
            .FirstOrDefault(fm => fm.UserId == userId && fm.MovieId == movieId);

        if (entry == null)
        {
            _logger.LogWarning("Favourite not found for user {UserId} and movie {MovieId}", userId, movieId);
            return;
        }

        context.FavouriteMovies.Remove(entry);
        context.SaveChanges();

        _logger.LogInformation("Removed movie {MovieId} from favourites for user {UserId}", movieId, userId);

        var cacheKey = $"favourites:user:{userId}";
        await _redis.DeleteKeyAsync(cacheKey);
        _logger.LogInformation("Cleared Redis cache for user {UserId} favourites", userId);
    }

    public async Task<List<FavouriteMovie>> GetFavouritesByUserIdAsync(int userId)
    {
        var cacheKey = $"favourites:user:{userId}";
        _logger.LogInformation("Checking Redis cache for favourites of user {UserId}", userId);

        var cachedJson = await _redis.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("Loaded favourites for user {UserId} from cache", userId);
            var favourites = System.Text.Json.JsonSerializer.Deserialize<List<FavouriteMovie>>(cachedJson)!;

            foreach (var fav in favourites)
            {
                fav.Movie = await _movieFacade.GetByIdAsync(fav.MovieId);
            }

            return favourites;
        }

        _logger.LogInformation("Querying SQL for favourites of user {UserId}", userId);
        using var context = ApplicationContextFactory.CreateDbContext();

        var entries = context.FavouriteMovies
            .Where(fm => fm.UserId == userId)
            .ToList();

        foreach (var fav in entries)
        {
            fav.Movie = await _movieFacade.GetByIdAsync(fav.MovieId);
        }

        // Cache stripped copy (without [NotMapped] property)
        var stripped = entries.Select(fm => new FavouriteMovie
        {
            Id = fm.Id,
            MovieId = fm.MovieId,
            UserId = fm.UserId
        }).ToList();

        var json = System.Text.Json.JsonSerializer.Serialize(stripped);
        await _redis.SetStringAsync(cacheKey, json, TimeSpan.FromSeconds(CacheTtlSeconds));

        _logger.LogInformation("Cached favourites for user {UserId}", userId);
        return entries;
    }
}
