using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SOFT_DB_EXAM.Hubs;

namespace SOFT_DB_EXAM.Facades
{
    public class RedisFacade
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;
        private readonly ISubscriber _subscriber;
        
        public RedisFacade(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _database = _connectionMultiplexer.GetDatabase();
            _subscriber = _connectionMultiplexer.GetSubscriber();
        }


        
        public async Task<string?> GetStringAsync(string key)
        {
            return await _database.StringGetAsync(key);
        }

        public async Task SetStringAsync(string key, string value, TimeSpan expiry)
        {
            await _database.StringSetAsync(key, value, expiry);
        }
        
        public async Task DeleteKeyAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }

        public async Task PublishAsync(string channel, string message)
        {
            await _subscriber.PublishAsync(channel, message);
        }

        
        public async Task SubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler)
        {
            await _subscriber.SubscribeAsync(channel, (redisChannel, redisValue) =>
            {
                handler(redisChannel, redisValue);
            });
        }
        
        public async Task SubscribeToPartyAsync(int partyId, IHubContext<WatchPartyHub> hubContext)
        {
            var channel = $"watchparty:{partyId}";
            await _subscriber.SubscribeAsync(channel, async (redisChannel, redisValue) =>
            {
                await hubContext.Clients.Group(channel).SendAsync("ReceiveMessage", redisValue.ToString());
            });
        }


    }
}
