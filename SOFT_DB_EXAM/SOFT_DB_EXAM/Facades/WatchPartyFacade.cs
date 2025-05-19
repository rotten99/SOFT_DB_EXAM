// src/Facades/WatchPartyFacade.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOFT_DB_EXAM.Entities;

namespace SOFT_DB_EXAM.Facades
{
    public class WatchPartyFacade
    {
        private readonly ApplicationDbContext _context;
        private readonly RedisFacade _redis;
        private readonly MovieFacade _movieFacade;
        private readonly ILogger<WatchPartyFacade> _logger;
        private const int UpcomingDaysWindow = 3;

        public WatchPartyFacade(
            ApplicationDbContext context,
            RedisFacade redis,
            MovieFacade movieFacade,
            ILogger<WatchPartyFacade> logger)
        {
            _context      = context;
            _redis        = redis;
            _movieFacade  = movieFacade;
            _logger       = logger;
        }

        public async Task<int> CreateWatchParty(string title, List<int> movieIds, List<int> userIds, DateTime start, DateTime end)
        {
            var party = new WatchParty
            {
                Title     = title,
                MovieIds  = movieIds,
                UserIds   = userIds,
                StartTime = start,
                EndTime   = end
            };

            _context.Add(party);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created watch party {PartyId}", party.Id);
            return party.Id;
        }

        public async Task<string?> JoinWatchPartyAsync(int partyId, int userId)
        {
            var party = await _context.WatchParties.FindAsync(partyId);
            var user  = await _context.Users.FindAsync(userId);

            if (party == null || user == null) return null;

            if (!party.UserIds.Contains(userId))
            {
                party.UserIds.Add(userId);
                await _context.SaveChangesAsync();
            }

            await _redis.PublishAsync($"watchparty:{partyId}",
                $"System: {user.UserName} joined watch party!");
            return user.UserName;
        }
        
        public async Task<WatchParty?> GetByIdAsync(int partyId)
        {
            // try cache first
            var cacheKey = $"watchparty:{partyId}";
            var json     = await _redis.GetStringAsync(cacheKey);
            WatchParty? party;

            if (!string.IsNullOrEmpty(json))
            {
                party = System.Text.Json.JsonSerializer.Deserialize<WatchParty>(json);
            }
            else
            {
                party = await _context.WatchParties.FindAsync(partyId);
                if (party == null) return null;

                // cache stripped version
                var stripped = new WatchParty {
                    Id        = party.Id,
                    Title     = party.Title,
                    MovieIds  = party.MovieIds,
                    UserIds   = party.UserIds,
                    StartTime = party.StartTime,
                    EndTime   = party.EndTime
                };
                var serialized = System.Text.Json.JsonSerializer.Serialize(stripped);
            }

            // hydrate movies & users
            if (party != null)
            {
                party.Movies = new List<Movie>();
                foreach (var mid in party.MovieIds)
                {
                    var m = await _movieFacade.GetByIdAsync(mid);
                    if (m != null) party.Movies.Add(m);
                }

                // load user names
                var users = await _context.Users
                    .Where(u => party.UserIds.Contains(u.Id))
                    .Select(u => new User { Id = u.Id, UserName = u.UserName })
                    .ToListAsync();
                party.Users = users;
            }

            return party;
        }
        
        public async Task<bool> SubscribeUserAsync(int partyId, int userId)
        {
            using var context = ApplicationContextFactory.CreateDbContext();

            var party = await context.WatchParties.FindAsync(partyId);
            var user = await context.Users.FindAsync(userId);

            if (party == null || user == null)
                throw new InvalidOperationException("Party or user not found");

            if (!party.UserIds.Contains(userId))
            {
                party.UserIds.Add(userId);
                await context.SaveChangesAsync();
                return true; // Newly subscribed
            }

            return false; // Already subscribed
        }


        public async Task BroadcastMessageAsync(int partyId, string message)
        {
            await _redis.PublishAsync($"watchparty:{partyId}", message);
        }

        /// <summary>
        /// Parties where StartTime ≤ now ≤ EndTime
        /// </summary>
        public async Task<List<WatchParty>> GetActiveWatchPartiesAsync()
        {
            var now = DateTime.UtcNow;
            var parties = await _context.WatchParties
                .Where(p => p.StartTime <= now && p.EndTime >= now)
                .ToListAsync();

            foreach (var p in parties)
                await HydratePartyAsync(p);

            return parties;
        }

        /// <summary>
        /// Parties where now &lt;= StartTime &lt;= now + 3 days
        /// </summary>
        public async Task<List<WatchParty>> GetUpcomingWatchPartiesAsync()
        {
            var now   = DateTime.UtcNow;
            var limit = now.AddDays(UpcomingDaysWindow);

            var parties = await _context.WatchParties
                .Where(p => p.StartTime > now && p.StartTime <= limit)
                .ToListAsync();

            foreach (var p in parties)
                await HydratePartyAsync(p);

            return parties;
        }

        /// <summary>
        /// Loads the Movie and User objects into the NotMapped properties
        /// </summary>
        private async Task HydratePartyAsync(WatchParty party)
        {
            // populate Movies
            party.Movies = new List<Movie>();
            foreach (var mid in party.MovieIds)
            {
                var m = await _movieFacade.GetByIdAsync(mid);
                if (m != null) party.Movies.Add(m);
            }

            // populate Users
            party.Users = await _context.Users
                .Where(u => party.UserIds.Contains(u.Id))
                .ToListAsync();
        }
    }
}
