using System.Text.Json;
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

    public FavouriteMovieFacade(
        ILogger<FavouriteMovieFacade> logger,
        RedisFacade redis,
        MovieFacade movieFacade)
    {
        _logger       = logger;
        _redis        = redis;
        _movieFacade  = movieFacade;
    }

    public async Task AddFavouriteAsync(int userId, int movieId)
    {
        using var context = ApplicationContextFactory.CreateDbContext();

        // 1) Insert into SQL if missing
        if (context.FavouriteMovies.Any(fm => fm.UserId == userId && fm.MovieId == movieId))
        {
            _logger.LogInformation("Movie {MovieId} already favorited by user {UserId}", movieId, userId);
            return;
        }

        var fav = new FavouriteMovie { UserId = userId, MovieId = movieId };
        context.FavouriteMovies.Add(fav);
        await context.SaveChangesAsync();
        _logger.LogInformation("Added movie {MovieId} to favourites for user {UserId}", movieId, userId);

        // 2) If cache exists, append to it
        var cacheKey   = $"favourites:user:{userId}";
        var cachedJson = await _redis.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("Updating Redis cache for user {UserId} favourites (add)", userId);

            var list = JsonSerializer.Deserialize<List<FavouriteMovie>>(cachedJson)!;
            // add the new stripped entry
            list.Add(new FavouriteMovie {
                Id      = fav.Id,
                UserId  = fav.UserId,
                MovieId = fav.MovieId
            });

            var newJson = JsonSerializer.Serialize(list);
            await _redis.SetStringAsync(cacheKey, newJson, TimeSpan.FromSeconds(CacheTtlSeconds));
        }
    }

    public async Task RemoveFavouriteAsync(int userId, int movieId)
    {
        using var context = ApplicationContextFactory.CreateDbContext();

        // 1) Remove from SQL
        var entry = await context.FavouriteMovies
                          .FirstOrDefaultAsync(fm => fm.UserId == userId && fm.MovieId == movieId);
        if (entry == null)
        {
            _logger.LogWarning("Favourite not found for user {UserId} and movie {MovieId}", userId, movieId);
            return;
        }
        context.FavouriteMovies.Remove(entry);
        await context.SaveChangesAsync();
        _logger.LogInformation("Removed movie {MovieId} from favourites for user {UserId}", movieId, userId);

        // 2) If cache exists, remove from it
        var cacheKey   = $"favourites:user:{userId}";
        var cachedJson = await _redis.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("Updating Redis cache for user {UserId} favourites (remove)", userId);

            var list = JsonSerializer.Deserialize<List<FavouriteMovie>>(cachedJson)!;
            var toRemove = list.FirstOrDefault(fm => fm.MovieId == movieId);
            if (toRemove != null)
            {
                list.Remove(toRemove);
                var newJson = JsonSerializer.Serialize(list);
                await _redis.SetStringAsync(cacheKey, newJson, TimeSpan.FromSeconds(CacheTtlSeconds));
            }
        }
    }

    public async Task<List<FavouriteMovie>> GetFavouritesByUserIdAsync(int userId)
    {
        var cacheKey = $"favourites:user:{userId}";
        _logger.LogInformation("Checking Redis cache for favourites of user {UserId}", userId);

        var cachedJson = await _redis.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("Loaded favourites for user {UserId} from cache", userId);
            var favourites = JsonSerializer.Deserialize<List<FavouriteMovie>>(cachedJson)!;

            // hydrate NotMapped Movie property
            foreach (var fav in favourites)
            {
                fav.Movie = await _movieFacade.GetByIdAsync(fav.MovieId);
            }
            return favourites;
        }

        _logger.LogInformation("Querying SQL for favourites of user {UserId}", userId);
        using var context = ApplicationContextFactory.CreateDbContext();

        var entries = await context.FavouriteMovies
            .Where(fm => fm.UserId == userId)
            .ToListAsync();

        foreach (var fav in entries)
        {
            fav.Movie = await _movieFacade.GetByIdAsync(fav.MovieId);
        }

        // Cache a stripped copy
        var stripped = entries.Select(fm => new FavouriteMovie
        {
            Id      = fm.Id,
            MovieId = fm.MovieId,
            UserId  = fm.UserId
        }).ToList();

        var json = JsonSerializer.Serialize(stripped);
        await _redis.SetStringAsync(cacheKey, json, TimeSpan.FromSeconds(CacheTtlSeconds));
        _logger.LogInformation("Cached favourites for user {UserId}", userId);

        return entries;
    }
}
