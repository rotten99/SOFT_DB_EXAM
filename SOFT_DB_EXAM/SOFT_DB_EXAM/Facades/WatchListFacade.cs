using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOFT_DB_EXAM.Entities;

namespace SOFT_DB_EXAM.Facades
{
    public class WatchListFacade
    {
        private readonly ILogger<WatchListFacade> _logger;
        private readonly RedisFacade _redis;
        private readonly MovieFacade _movieFacade;
        private const int CacheTtlSeconds = 300; // 5 minutes

        public WatchListFacade(
            ILogger<WatchListFacade> logger,
            RedisFacade redis,
            MovieFacade movieFacade)
        {
            _logger      = logger;
            _redis       = redis;
            _movieFacade = movieFacade;
        }

        public int CreateWatchList(string name, bool isPrivate, int userId)
        {
            using var context = ApplicationContextFactory.CreateDbContext();
            var watchList = new WatchList
            {
                Name      = name,
                IsPrivate = isPrivate,
                UserId    = userId,
                AddedDate = DateTime.UtcNow
            };

            context.WatchLists.Add(watchList);
            context.SaveChanges();

            _logger.LogInformation(
                "Created watchlist {WatchListId} for user {UserId}",
                watchList.Id, userId);

            // Invalidate caches that list watchlists
            _redis.DeleteKeyAsync("watchlists:all").Wait();
            _redis.DeleteKeyAsync($"watchlists:user:{userId}").Wait();

            return watchList.Id;
        }

        public async Task AddMoviesToWatchListAsync(int watchListId, List<int> movieIds)
        {
            using var context = ApplicationContextFactory.CreateDbContext();
            var watchList = await context.WatchLists
                .Include(wl => wl.ListedMovies)
                .FirstOrDefaultAsync(wl => wl.Id == watchListId);

            if (watchList == null)
                throw new InvalidOperationException("Watchlist not found.");

            foreach (var movieId in movieIds)
            {
                if (watchList.ListedMovies.Any(lm => lm.MovieId == movieId))
                    continue;

                var movie = await _movieFacade.GetByIdAsync(movieId);
                if (movie == null) continue;

                watchList.ListedMovies.Add(new ListedMovie
                {
                    MovieId    = movieId,
                    HasWatched = false,
                    Movie      = movie
                });
            }

            context.SaveChanges();
            _logger.LogInformation(
                "Added movies to watchlist {WatchListId}", watchListId);

            // Invalidate caches for this list and for listings
            await _redis.DeleteKeyAsync($"watchlist:{watchListId}");
            await _redis.DeleteKeyAsync($"watchlists:user:{watchList.UserId}");
            await _redis.DeleteKeyAsync("watchlists:all");
        }

        public async Task<List<WatchList>> GetWatchListsByUserIdAsync(int userId)
        {
            var cacheKey = $"watchlists:user:{userId}";
            var cached   = await _redis.GetStringAsync(cacheKey);
            List<WatchList> lists;

            if (!string.IsNullOrEmpty(cached))
            {
                lists = JsonSerializer.Deserialize<List<WatchList>>(cached)!;
            }
            else
            {
                await using var ctx = ApplicationContextFactory.CreateDbContext();
                lists = await ctx.WatchLists
                    .Include(wl => wl.ListedMovies)
                    .Where(wl => wl.UserId == userId)
                    .ToListAsync();

                var stripped = lists.Select(wl => new WatchList
                {
                    Id           = wl.Id,
                    Name         = wl.Name,
                    IsPrivate    = wl.IsPrivate,
                    UserId       = wl.UserId,
                    AddedDate    = wl.AddedDate,
                    ListedMovies = wl.ListedMovies
                        .Select(lm => new ListedMovie
                        {
                            Id         = lm.Id,
                            MovieId    = lm.MovieId,
                            HasWatched = lm.HasWatched
                        })
                        .ToList()
                }).ToList();

                await _redis.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(stripped),
                    TimeSpan.FromSeconds(CacheTtlSeconds));
            }

            await HydrateMoviesAndOwnerAsync(lists);
            return lists;
        }

        public async Task FollowWatchListAsync(int userId, int watchListId)
        {
            using var context = ApplicationContextFactory.CreateDbContext();
            var exists = context.WatchListsFollowed
                .Any(wf => wf.UserId == userId && wf.WatchListId == watchListId);
            if (exists) return;

            context.WatchListsFollowed.Add(new WatchListsFollowed
            {
                UserId      = userId,
                WatchListId = watchListId
            });
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} followed WatchList {WatchListId}",
                userId, watchListId);

            // Invalidate this user's followed‐lists cache
            await _redis.DeleteKeyAsync($"watchlists:followed:user:{userId}");
        }

        public async Task UnfollowWatchListAsync(int userId, int watchListId)
        {
            using var context = ApplicationContextFactory.CreateDbContext();
            var entry = await context.WatchListsFollowed
                .FirstOrDefaultAsync(wf => wf.UserId == userId && wf.WatchListId == watchListId);
            if (entry != null)
            {
                context.WatchListsFollowed.Remove(entry);
                await context.SaveChangesAsync();
                _logger.LogInformation(
                    "User {UserId} unfollowed WatchList {WatchListId}",
                    userId, watchListId);
                await _redis.DeleteKeyAsync($"watchlists:followed:user:{userId}");
            }
        }

        public async Task<List<WatchList>> GetFollowedWatchListsByUserIdAsync(int userId)
        {
            var cacheKey = $"watchlists:followed:user:{userId}";
            var cached   = await _redis.GetStringAsync(cacheKey);
            List<WatchList> lists;

            if (!string.IsNullOrEmpty(cached))
            {
                lists = JsonSerializer.Deserialize<List<WatchList>>(cached)!;
            }
            else
            {
                await using var ctx = ApplicationContextFactory.CreateDbContext();
                var followedIds = await ctx.WatchListsFollowed
                    .Where(wf => wf.UserId == userId)
                    .Select(wf => wf.WatchListId)
                    .ToListAsync();

                lists = await ctx.WatchLists
                    .Include(wl => wl.ListedMovies)
                    .Where(wl => followedIds.Contains(wl.Id))
                    .ToListAsync();

                var stripped = lists.Select(wl => new WatchList
                {
                    Id           = wl.Id,
                    Name         = wl.Name,
                    IsPrivate    = wl.IsPrivate,
                    UserId       = wl.UserId,
                    AddedDate    = wl.AddedDate,
                    ListedMovies = wl.ListedMovies
                        .Select(lm => new ListedMovie
                        {
                            Id         = lm.Id,
                            MovieId    = lm.MovieId,
                            HasWatched = lm.HasWatched
                        })
                        .ToList()
                }).ToList();

                await _redis.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(stripped),
                    TimeSpan.FromSeconds(CacheTtlSeconds));
            }

            await HydrateMoviesAndOwnerAsync(lists);
            return lists;
        }

        public async Task<List<WatchList>> GetAllWatchListsAsync()
        {
            const string cacheKey = "watchlists:all";
            var cached = await _redis.GetStringAsync(cacheKey);
            List<WatchList> lists;

            if (!string.IsNullOrEmpty(cached))
            {
                lists = JsonSerializer.Deserialize<List<WatchList>>(cached)!;
            }
            else
            {
                await using var ctx = ApplicationContextFactory.CreateDbContext();
                lists = await ctx.WatchLists
                    .Include(wl => wl.ListedMovies)
                    .ToListAsync();

                var stripped = lists.Select(wl => new WatchList
                {
                    Id           = wl.Id,
                    Name         = wl.Name,
                    IsPrivate    = wl.IsPrivate,
                    UserId       = wl.UserId,
                    AddedDate    = wl.AddedDate,
                    ListedMovies = wl.ListedMovies
                        .Select(lm => new ListedMovie
                        {
                            Id         = lm.Id,
                            MovieId    = lm.MovieId,
                            HasWatched = lm.HasWatched
                        })
                        .ToList()
                }).ToList();

                await _redis.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(stripped),
                    TimeSpan.FromSeconds(CacheTtlSeconds));
            }

            await HydrateMoviesAndOwnerAsync(lists);
            return lists;
        }

        public async Task<WatchList?> GetWatchListByIdAsync(int watchListId)
        {
            var cacheKey = $"watchlist:{watchListId}";
            var cached   = await _redis.GetStringAsync(cacheKey);
            WatchList? wl;

            if (!string.IsNullOrEmpty(cached))
            {
                wl = JsonSerializer.Deserialize<WatchList>(cached)!;
            }
            else
            {
                await using var ctx = ApplicationContextFactory.CreateDbContext();
                wl = await ctx.WatchLists
                    .Include(wl2 => wl2.ListedMovies)
                    .FirstOrDefaultAsync(wl2 => wl2.Id == watchListId);

                if (wl == null) return null;

                var stripped = new WatchList
                {
                    Id           = wl.Id,
                    Name         = wl.Name,
                    IsPrivate    = wl.IsPrivate,
                    UserId       = wl.UserId,
                    AddedDate    = wl.AddedDate,
                    ListedMovies = wl.ListedMovies
                        .Select(lm => new ListedMovie
                        {
                            Id         = lm.Id,
                            MovieId    = lm.MovieId,
                            HasWatched = lm.HasWatched
                        })
                        .ToList()
                };

                await _redis.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(stripped),
                    TimeSpan.FromSeconds(CacheTtlSeconds));
            }

            if (wl != null)
                await HydrateMoviesAndOwnerAsync(new[] { wl });

            return wl;
        }

        public async Task RemoveMovieFromWatchListAsync(int watchListId, int movieId)
        {
            using var context = ApplicationContextFactory.CreateDbContext();
            var wl = await context.WatchLists
                .Include(wl2 => wl2.ListedMovies)
                .FirstOrDefaultAsync(wl2 => wl2.Id == watchListId);

            if (wl == null) throw new InvalidOperationException("Watchlist not found.");

            var lm = wl.ListedMovies.FirstOrDefault(x => x.MovieId == movieId);
            if (lm != null)
            {
                wl.ListedMovies.Remove(lm);
                context.SaveChanges();
                _logger.LogInformation(
                    "Removed Movie {MovieId} from WatchList {WatchListId}",
                    movieId, watchListId);

                // Invalidate caches
                await _redis.DeleteKeyAsync($"watchlist:{watchListId}");
                await _redis.DeleteKeyAsync($"watchlists:user:{wl.UserId}");
                await _redis.DeleteKeyAsync("watchlists:all");
            }
        }

        /// <summary>
        /// Populate each WatchList.User (with Id+UserName) and each ListedMovie.Movie
        /// </summary>
        private async Task HydrateMoviesAndOwnerAsync(IEnumerable<WatchList> lists)
        {
            var userIds = lists.Select(wl => wl.UserId).Distinct().ToList();
            await using var ctx = ApplicationContextFactory.CreateDbContext();
            var users = await ctx.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToDictionaryAsync(x => x.Id, x => x.UserName);

            foreach (var wl in lists)
            {
                wl.User = new User
                {
                    Id       = wl.UserId,
                    UserName = users.GetValueOrDefault(wl.UserId, "Unknown")
                };
                foreach (var lm in wl.ListedMovies)
                    lm.Movie = await _movieFacade.GetByIdAsync(lm.MovieId);
            }
        }
    }
}
