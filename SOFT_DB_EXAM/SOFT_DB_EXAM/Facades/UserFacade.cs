
using Microsoft.EntityFrameworkCore;

namespace SOFT_DB_EXAM.Facades;

public class UserFacade 
{
    private ILogger<UserFacade> _logger;
    private readonly RedisFacade _redis;
    private const int CacheTtlSeconds = 300; // 5 minutes
    
    public UserFacade(ILogger<UserFacade> logger, RedisFacade redis)
    {
        _logger = logger;
        _redis = redis;
    }
    
    
    
    
}