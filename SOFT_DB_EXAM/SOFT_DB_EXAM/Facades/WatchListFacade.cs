using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOFT_DB_EXAM.Entities;

namespace SOFT_DB_EXAM.Facades;

public class WatchListFacade
{
    private readonly ILogger<WatchListFacade> _logger;
    private readonly RedisFacade _redis;
    private const int CacheTtlSeconds = 300; // 5 minutes
    private readonly MovieFacade _movieFacade;


    public WatchListFacade(ILogger<WatchListFacade> logger, RedisFacade redis, MovieFacade movieFacade)
    {
        _movieFacade = movieFacade;
        _logger = logger;
        _redis = redis;
    }


    public int CreateWatchList(string name, bool isPrivate, int userId)
    {
        using var context = ApplicationContextFactory.CreateDbContext();

        var watchList = new WatchList
        {
            Name = name,
            IsPrivate = isPrivate,
            UserId = userId,
            AddedDate = DateTime.UtcNow
        };

        context.WatchLists.Add(watchList);
        context.SaveChanges();

        _logger.LogInformation("Created watchlist {WatchListId} for user {UserId}", watchList.Id, userId);
        return watchList.Id;
    }


    public async Task AddMoviesToWatchListAsync(int watchListId, List<int> movieIds)
    {
        using var context = ApplicationContextFactory.CreateDbContext();

        var watchList = context.WatchLists
            .Include(wl => wl.ListedMovies)
            .FirstOrDefault(wl => wl.Id == watchListId);

        if (watchList == null)
        {
            _logger.LogWarning("WatchList {WatchListId} not found.", watchListId);
            throw new InvalidOperationException("Watchlist not found.");
        }

        foreach (var movieId in movieIds)
        {
            // Skip if already added
            if (watchList.ListedMovies.Any(lm => lm.MovieId == movieId))
                continue;

            var movie = await _movieFacade.GetByIdAsync(movieId);
            if (movie == null)
            {
                _logger.LogWarning("Movie with ID {MovieId} not found in MongoDB. Skipping.", movieId);
                continue;
            }

            var listedMovie = new ListedMovie
            {
                MovieId = movieId,
                HasWatched = false,
                Movie = movie
            };

            watchList.ListedMovies.Add(listedMovie);
        }

        context.SaveChanges();
        _logger.LogInformation("Added movies to watchlist {WatchListId}", watchListId);
    }

    public async Task<List<WatchList>> GetWatchListsByUserIdAsync(int userId)
    {
        var cacheKey = $"watchlists:user:{userId}";
        _logger.LogInformation("Checking Redis cache for watchlists by user {UserId}", userId);

        var cachedJson = await _redis.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("Loaded watchlists for user {UserId} from cache", userId);
            var cached = System.Text.Json.JsonSerializer.Deserialize<List<WatchList>>(cachedJson)!;

            // Hydrate movie details
            foreach (var wl in cached)
            {
                foreach (var lm in wl.ListedMovies)
                {
                    lm.Movie = await _movieFacade.GetByIdAsync(lm.MovieId);
                }
            }

            return cached;
        }

        _logger.LogInformation("Querying SQL for watchlists by user {UserId}", userId);
        using var context = ApplicationContextFactory.CreateDbContext();

        var watchLists = context.WatchLists
            .Include(wl => wl.ListedMovies)
            .Where(wl => wl.UserId == userId)
            .ToList();

        foreach (var watchList in watchLists)
        {
            foreach (var listedMovie in watchList.ListedMovies)
            {
                listedMovie.Movie = await _movieFacade.GetByIdAsync(listedMovie.MovieId);
            }
        }

        // Cache the raw SQL result (excluding Movie object)
        var stripped = watchLists.Select(wl => new WatchList
        {
            Id = wl.Id,
            AddedDate = wl.AddedDate,
            Name = wl.Name,
            IsPrivate = wl.IsPrivate,
            UserId = wl.UserId,
            ListedMovies = wl.ListedMovies.Select(lm => new ListedMovie
            {
                Id = lm.Id,
                MovieId = lm.MovieId,
                HasWatched = lm.HasWatched
            }).ToList()
        }).ToList();

        var json = System.Text.Json.JsonSerializer.Serialize(stripped);
        await _redis.SetStringAsync(cacheKey, json, TimeSpan.FromSeconds(CacheTtlSeconds));

        _logger.LogInformation("Cached watchlists for user {UserId}", userId);
        return watchLists;
    }

    public async Task FollowWatchListAsync(int userId, int watchListId)
    {
        using var context = ApplicationContextFactory.CreateDbContext();

        var exists = context.WatchListsFollowed
            .Any(wf => wf.UserId == userId && wf.WatchListId == watchListId);

        if (exists)
        {
            _logger.LogInformation("User {UserId} already follows WatchList {WatchListId}", userId, watchListId);
            return;
        }

        var watchList = context.WatchLists.FirstOrDefault(w => w.Id == watchListId);
        var user = context.Users.FirstOrDefault(u => u.Id == userId);

        if (watchList == null || user == null)
        {
            _logger.LogWarning("Follow failed: Invalid user ({UserId}) or watchlist ({WatchListId})", userId,
                watchListId);
            throw new InvalidOperationException("User or watchlist not found.");
        }

        var follow = new WatchListsFollowed
        {
            UserId = userId,
            WatchListId = watchListId
        };

        context.WatchListsFollowed.Add(follow);
        await context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} now follows WatchList {WatchListId}", userId, watchListId);
    }

    public async Task UnfollowWatchListAsync(int userId, int watchListId)
    {
        using var context = ApplicationContextFactory.CreateDbContext();

        var entry = context.WatchListsFollowed
            .FirstOrDefault(wf => wf.UserId == userId && wf.WatchListId == watchListId);

        if (entry == null)
        {
            _logger.LogWarning("User {UserId} is not following WatchList {WatchListId}", userId, watchListId);
            return;
        }

        context.WatchListsFollowed.Remove(entry);
        await context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} unfollowed WatchList {WatchListId}", userId, watchListId);

        var cacheKey = $"watchlists:followed:user:{userId}";
        await _redis.DeleteKeyAsync(cacheKey);
        _logger.LogInformation("Cleared Redis cache for followed watchlists of user {UserId}", userId);
    }


    public async Task<List<WatchList>> GetFollowedWatchListsByUserIdAsync(int userId)
    {
        var cacheKey = $"watchlists:followed:user:{userId}";
        _logger.LogInformation("Checking Redis cache for followed watchlists by user {UserId}", userId);

        var cachedJson = await _redis.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("Loaded followed watchlists for user {UserId} from cache", userId);
            var cached = System.Text.Json.JsonSerializer.Deserialize<List<WatchList>>(cachedJson)!;

            foreach (var wl in cached)
            {
                foreach (var lm in wl.ListedMovies)
                {
                    lm.Movie = await _movieFacade.GetByIdAsync(lm.MovieId);
                }
            }

            return cached;
        }

        using var context = ApplicationContextFactory.CreateDbContext();

        var followedIds = context.WatchListsFollowed
            .Where(wf => wf.UserId == userId)
            .Select(wf => wf.WatchListId)
            .ToList();

        var watchLists = context.WatchLists
            .Include(wl => wl.ListedMovies)
            .Where(wl => followedIds.Contains(wl.Id))
            .ToList();

        foreach (var watchList in watchLists)
        {
            foreach (var lm in watchList.ListedMovies)
            {
                lm.Movie = await _movieFacade.GetByIdAsync(lm.MovieId);
            }
        }

        // Strip NotMapped movies for cache
        var stripped = watchLists.Select(wl => new WatchList
        {
            Id = wl.Id,
            AddedDate = wl.AddedDate,
            Name = wl.Name,
            IsPrivate = wl.IsPrivate,
            UserId = wl.UserId,
            ListedMovies = wl.ListedMovies.Select(lm => new ListedMovie
            {
                Id = lm.Id,
                MovieId = lm.MovieId,
                HasWatched = lm.HasWatched
            }).ToList()
        }).ToList();

        var json = System.Text.Json.JsonSerializer.Serialize(stripped);
        await _redis.SetStringAsync(cacheKey, json, TimeSpan.FromMinutes(5));

        _logger.LogInformation("Cached followed watchlists for user {UserId}", userId);
        return watchLists;
    }
    
    public async Task<WatchList?> GetWatchListByIdAsync(int watchListId)
    {
        var cacheKey = $"watchlist:{watchListId}";
        _logger.LogInformation("Checking Redis cache for WatchList {WatchListId}", watchListId);

        var cachedJson = await _redis.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("Loaded WatchList {WatchListId} from cache", watchListId);
            var cached = System.Text.Json.JsonSerializer.Deserialize<WatchList>(cachedJson)!;

            foreach (var lm in cached.ListedMovies)
            {
                lm.Movie = await _movieFacade.GetByIdAsync(lm.MovieId);
            }

            return cached;
        }

        using var context = ApplicationContextFactory.CreateDbContext();

        var watchList = context.WatchLists
            .Include(wl => wl.ListedMovies)
            .FirstOrDefault(wl => wl.Id == watchListId);

        if (watchList == null)
        {
            _logger.LogWarning("WatchList {WatchListId} not found in database", watchListId);
            return null;
        }

        foreach (var lm in watchList.ListedMovies)
        {
            lm.Movie = await _movieFacade.GetByIdAsync(lm.MovieId);
        }

        // Strip NotMapped Movie before caching
        var stripped = new WatchList
        {
            Id = watchList.Id,
            AddedDate = watchList.AddedDate,
            Name = watchList.Name,
            IsPrivate = watchList.IsPrivate,
            UserId = watchList.UserId,
            ListedMovies = watchList.ListedMovies.Select(lm => new ListedMovie
            {
                Id = lm.Id,
                MovieId = lm.MovieId,
                HasWatched = lm.HasWatched
            }).ToList()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(stripped);
        await _redis.SetStringAsync(cacheKey, json, TimeSpan.FromMinutes(5));

        _logger.LogInformation("Cached WatchList {WatchListId}", watchListId);
        return watchList;
    }
    
    public async Task RemoveMovieFromWatchListAsync(int watchListId, int movieId)
    {
        using var context = ApplicationContextFactory.CreateDbContext();

        var watchList = context.WatchLists
            .Include(wl => wl.ListedMovies)
            .FirstOrDefault(wl => wl.Id == watchListId);

        if (watchList == null)
        {
            _logger.LogWarning("WatchList {WatchListId} not found", watchListId);
            throw new InvalidOperationException("WatchList not found");
        }

        var movieToRemove = watchList.ListedMovies.FirstOrDefault(lm => lm.MovieId == movieId);
        if (movieToRemove == null)
        {
            _logger.LogWarning("Movie {MovieId} not found in WatchList {WatchListId}", movieId, watchListId);
            return;
        }

        watchList.ListedMovies.Remove(movieToRemove);
        context.SaveChanges();

        _logger.LogInformation("Removed Movie {MovieId} from WatchList {WatchListId}", movieId, watchListId);

        // Invalidate cache for the full watchlist and the user’s watchlists
        var listCacheKey = $"watchlist:{watchListId}";
        var userCacheKey = $"watchlists:user:{watchList.UserId}";

        await _redis.DeleteKeyAsync(listCacheKey);
        await _redis.DeleteKeyAsync(userCacheKey);
    
        _logger.LogInformation("Cleared Redis cache for WatchList {WatchListId} and user {UserId}", watchListId, watchList.UserId);
    }


}