using SOFT_DB_EXAM.Entities;

namespace SOFT_DB_EXAM.Facades;

public class WatchPartyFacade
{
    private readonly ApplicationDbContext _context;
    private readonly RedisFacade _redis;
    private readonly MovieFacade _movieFacade;
    private readonly ILogger<WatchPartyFacade> _logger;
    
    public WatchPartyFacade(
        ApplicationDbContext context,
        RedisFacade redis,
        MovieFacade movieFacade,
        ILogger<WatchPartyFacade> logger)
    {
        _context = context;
        _redis = redis;
        _movieFacade = movieFacade;
        _logger = logger;
    }

    public async Task<int> CreateWatchParty(string title, List<int> movieIds, List<int> userIds, DateTime start, DateTime end)
    {
        var party = new WatchParty
        {
            Title = title,
            MovieIds = movieIds,
            UserIds = userIds,
            StartTime = start,
            EndTime = end
        };

        _context.Add(party);
        await _context.SaveChangesAsync();
        return party.Id;
    }

    public async Task<string?> JoinWatchPartyAsync(int partyId, int userId)
    {
        var party = await _context.WatchParties.FindAsync(partyId);
        var user = await _context.Users.FindAsync(userId);

        if (party == null || user == null) return null;

        if (!party.UserIds.Contains(userId))
        {
            party.UserIds.Add(userId);
            await _context.SaveChangesAsync();
        }

        await _redis.PublishAsync($"watchparty:{partyId}", $"System: {user.UserName} joined the watch party!");
        return user.UserName;
    }
    
    public async Task<bool> SubscribeUserAsync(int partyId, int userId)
    {
        using var context = ApplicationContextFactory.CreateDbContext();

        var party = await context.WatchParties.FindAsync(partyId);
        var user = await context.Users.FindAsync(userId);

        if (party == null || user == null)
            return false;

        if (party.UserIds.Contains(userId))
            return false;

        party.UserIds.Add(userId);
        await context.SaveChangesAsync();
        return true;
    }



    public async Task BroadcastMessageAsync(int partyId, string message)
    {
        await _redis.PublishAsync($"watchparty:{partyId}", message);
    }
}
