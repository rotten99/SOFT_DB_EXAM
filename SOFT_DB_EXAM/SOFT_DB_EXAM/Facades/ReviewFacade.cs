using Microsoft.AspNetCore.Components.Web;

namespace SOFT_DB_EXAM.Facades;

public class ReviewFacade
{
    private readonly ILogger<ReviewFacade> _logger;
    private readonly RedisFacade _redis;
    private const int CacheTtlSeconds = 300; // 5 minutes
    
    public ReviewFacade(ILogger<ReviewFacade> logger, RedisFacade redis)
    {
        _redis = redis;
        _logger = logger;
    }
    
    
}