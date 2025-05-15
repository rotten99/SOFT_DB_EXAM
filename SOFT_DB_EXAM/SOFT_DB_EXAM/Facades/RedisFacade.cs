using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SOFT_DB_EXAM.Facades
{
    public class RedisFacade
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;

        public RedisFacade(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _database = _connectionMultiplexer.GetDatabase();
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


    }
}
