using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SOFT_DB_EXAM.Facades;

public class WatchListFacade
{
    private readonly ILogger<WatchListFacade> _logger;

    public WatchListFacade(ILogger<WatchListFacade> logger)
    {
        _logger = logger;
    }

    public List<WatchList> GetAllOwnedWatchListByUserId(int userId)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Fetching owned watch lists for user {UserId}", userId);

                    var watchLists = context.WatchLists
                        .Include(w => w.ListedMovies)
                        .Include(w => w.User)
                        .Where(w => w.UserId == userId)
                        .ToList();

                    transaction.Commit();
                    _logger.LogInformation("Successfully fetched {Count} owned watch lists for user {UserId}", watchLists.Count, userId);

                    return watchLists;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch owned watch lists for user {UserId}", userId);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    public List<WatchList> GetAllFollowedWatchListByUserId(int userId)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Fetching followed watch lists for user {UserId}", userId);

                    var followedWatchLists = context.WatchListsFollowed
                        .Include(w => w.WatchList)
                        .ThenInclude(w => w.User)
                        .Where(w => w.UserId == userId)
                        .Select(w => w.WatchList)
                        .ToList();

                    transaction.Commit();
                    _logger.LogInformation("Successfully fetched {Count} followed watch lists for user {UserId}", followedWatchLists.Count, userId);

                    return followedWatchLists;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch followed watch lists for user {UserId}", userId);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    public void CreateOrUpdateWatchList(WatchList watchList)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Creating or updating watch list with ID {WatchListId}", watchList.Id);

                    if (watchList.Id == 0)
                    {
                        context.WatchLists.Add(watchList);
                    }
                    else
                    {
                        context.WatchLists.Update(watchList);
                    }

                    context.SaveChanges();
                    transaction.Commit();
                    _logger.LogInformation("Successfully created or updated watch list with ID {WatchListId}", watchList.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create or update watch list with ID {WatchListId}", watchList.Id);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}
