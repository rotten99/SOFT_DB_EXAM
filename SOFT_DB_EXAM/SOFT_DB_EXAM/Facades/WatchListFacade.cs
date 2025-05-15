using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SOFT_DB_EXAM.Facades;

public class WatchListFacade
{
    private readonly ILogger<WatchListFacade> _logger;
    private readonly RedisFacade _redis;
    private const int CacheTtlSeconds = 300; // 5 minutes

    public WatchListFacade(ILogger<WatchListFacade> logger, RedisFacade redis)
    {
        _redis = redis;
        _logger = logger;
    }
    

   
}
