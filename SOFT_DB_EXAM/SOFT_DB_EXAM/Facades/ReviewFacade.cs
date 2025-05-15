using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;

namespace SOFT_DB_EXAM.Facades;

public class ReviewFacade
{
    private readonly ILogger<ReviewFacade> _logger;
    private readonly RedisFacade _redis;
    private const int CacheTtlSeconds = 300; // 5 minutes
    
    private readonly MovieFacade _movieFacade;

    public ReviewFacade(ILogger<ReviewFacade> logger, RedisFacade redis, MovieFacade movieFacade)
    {
        _logger = logger;
        _redis = redis;
        _movieFacade = movieFacade;
    }

    
    public void CreateReview(string reviewText, int rating, int movieId, int userId, string title)
    {
        using var context = ApplicationContextFactory.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        try
        {
            _logger.LogInformation("Creating review for movie {MovieId} by user {UserId}", movieId, userId);

            // Use ExecuteSqlRaw to bypass OUTPUT clause limitation
            context.Database.ExecuteSqlRaw(@"
            INSERT INTO Reviews (UserId, MovieId, Title, Description, Rating)
            VALUES (@p0, @p1, @p2, @p3, @p4);
        ", userId, movieId, title, reviewText, rating);

            transaction.Commit();
            _logger.LogInformation("Successfully created review for movie {MovieId} by user {UserId}", movieId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create review for movie {MovieId} by user {UserId}", movieId, userId);
            transaction.Rollback();
            throw;
        }
    }

    
    public async Task<List<Review>> GetReviewsByUserIdAsync(int userId)
    {
        var cacheKey = $"reviews:user:{userId}";
        _logger.LogInformation("Checking Redis cache for key: {Key}", cacheKey);

        var cachedJson = await _redis.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("Loaded reviews for user {UserId} from cache.", userId);
            var reviews = System.Text.Json.JsonSerializer.Deserialize<List<Review>>(cachedJson)!;

            foreach (var review in reviews)
            {
                review.Movie = await _movieFacade.GetByIdAsync(review.MovieId);
            }

            return reviews;
        }

        _logger.LogInformation("Querying database for reviews by user {UserId}", userId);
        using var context = ApplicationContextFactory.CreateDbContext();
        var dbReviews = context.Reviews.Where(r => r.UserId == userId).ToList();

        foreach (var review in dbReviews)
        {
            review.Movie = await _movieFacade.GetByIdAsync(review.MovieId);
        }

        var json = System.Text.Json.JsonSerializer.Serialize(dbReviews);
        await _redis.SetStringAsync(cacheKey, json, TimeSpan.FromSeconds(CacheTtlSeconds));

        _logger.LogInformation("Cached reviews for user {UserId}", userId);
        return dbReviews;
    }
    
    public async Task<List<Review>> GetReviewsByMovieIdAsync(int movieId)
    {
        var cacheKey = $"reviews:movie:{movieId}";
        _logger.LogInformation("Checking Redis cache for key: {Key}", cacheKey);

        var cachedJson = await _redis.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("Loaded reviews for movie {MovieId} from cache.", movieId);
            var reviews = System.Text.Json.JsonSerializer.Deserialize<List<Review>>(cachedJson)!;

            var movie = await _movieFacade.GetByIdAsync(movieId);
            foreach (var review in reviews)
            {
                review.Movie = movie;
            }

            return reviews;
        }

        _logger.LogInformation("Querying database for reviews by movie {MovieId}", movieId);
        using var context = ApplicationContextFactory.CreateDbContext();
        var dbReviews = context.Reviews.Where(r => r.MovieId == movieId).ToList();

        var movieFromMongo = await _movieFacade.GetByIdAsync(movieId);
        foreach (var review in dbReviews)
        {
            review.Movie = movieFromMongo;
        }

        var json = System.Text.Json.JsonSerializer.Serialize(dbReviews);
        await _redis.SetStringAsync(cacheKey, json, TimeSpan.FromSeconds(CacheTtlSeconds));

        _logger.LogInformation("Cached reviews for movie {MovieId}", movieId);
        return dbReviews;
    }

    public async Task<AverageRating?> GetAverageRatingByMovieIdAsync(int movieId)
    {
        var cacheKey = $"average-rating:{movieId}";
        _logger.LogInformation("Checking Redis for average rating key: {CacheKey}", cacheKey);

        var cachedJson = await _redis.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("Loaded average rating for movie {MovieId} from cache.", movieId);
            var rating = System.Text.Json.JsonSerializer.Deserialize<AverageRating>(cachedJson)!;
            rating.Movie = await _movieFacade.GetByIdAsync(movieId);
            return rating;
        }

        _logger.LogInformation("Querying SQL for average rating of movie {MovieId}", movieId);
        using var context = ApplicationContextFactory.CreateDbContext();

        var dbRating = context.AverageRatings.FirstOrDefault(r => r.MovieId == movieId);
        if (dbRating == null)
        {
            _logger.LogWarning("No average rating found in SQL for movie {MovieId}", movieId);
            return null;
        }

        dbRating.Movie = await _movieFacade.GetByIdAsync(movieId);

        var json = System.Text.Json.JsonSerializer.Serialize(dbRating);
        await _redis.SetStringAsync(cacheKey, json, TimeSpan.FromMinutes(5));

        _logger.LogInformation("Cached average rating for movie {MovieId}", movieId);
        return dbRating;
    }

    public async Task<Review?> GetReviewByIdAsync(int reviewId)
    {
        var cacheKey = $"reviews:{reviewId}";
        _logger.LogInformation("Checking Redis cache for review {ReviewId}", reviewId);

        var cachedJson = await _redis.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("Loaded review {ReviewId} from cache", reviewId);
            var review = System.Text.Json.JsonSerializer.Deserialize<Review>(cachedJson)!;
            review.Movie = await _movieFacade.GetByIdAsync(review.MovieId);
            return review;
        }

        using var context = ApplicationContextFactory.CreateDbContext();

        var dbReview = context.Reviews.FirstOrDefault(r => r.Id == reviewId);
        if (dbReview == null)
        {
            _logger.LogWarning("Review {ReviewId} not found in database", reviewId);
            return null;
        }

        dbReview.Movie = await _movieFacade.GetByIdAsync(dbReview.MovieId);

        // Strip Movie before caching
        var stripped = new Review
        {
            Id = dbReview.Id,
            UserId = dbReview.UserId,
            MovieId = dbReview.MovieId,
            Title = dbReview.Title,
            Description = dbReview.Description,
            Rating = dbReview.Rating
        };

        var json = System.Text.Json.JsonSerializer.Serialize(stripped);
        await _redis.SetStringAsync(cacheKey, json, TimeSpan.FromMinutes(5));

        _logger.LogInformation("Cached review {ReviewId}", reviewId);
        return dbReview;
    }
   
}