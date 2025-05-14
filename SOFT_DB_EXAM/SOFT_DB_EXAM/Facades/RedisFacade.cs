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

        /// <summary>
        /// Stores a set of key-value pairs in a Redis hash.
        /// </summary>
        public async Task SetHashAsync(string key, Dictionary<string, string> values)
        {
            var hashEntries = new List<HashEntry>();

            foreach (var kvp in values)
            {
                hashEntries.Add(new HashEntry(kvp.Key, kvp.Value));
            }

            await _database.HashSetAsync(key, hashEntries.ToArray());
        }

        /// <summary>
        /// Retrieves all fields from a Redis hash.
        /// </summary>
        public async Task<Dictionary<string, string>> GetHashAsync(string key)
        {
            var hashEntries = await _database.HashGetAllAsync(key);

            var result = new Dictionary<string, string>();

            foreach (var entry in hashEntries)
            {
                result.Add(entry.Name, entry.Value);
            }

            return result;
        }
    }
}
