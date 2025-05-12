using Microsoft.AspNetCore.Components.Web;

namespace SOFT_DB_EXAM.Facades;

public class ReviewFacade
{
    private readonly ILogger<ReviewFacade> _logger;
    
    public ReviewFacade(ILogger<ReviewFacade> logger)
    {
        _logger = logger;
    }
    
    public void CreateReview(string reviewText, int rating, int movieId, int userId, string title)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Creating review for movie {MovieId} by user {UserId}", movieId, userId);

                    var review = new Review
                    {
                        Description = reviewText,
                        Rating = rating,
                        MovieId = movieId,
                        UserId = userId,
                        Title = title
                    };

                    context.Reviews.Add(review);
                    context.SaveChanges();

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
        }
    }
    
    public List<Review> GetAllReviewsByMovieId(int movieId)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Fetching all reviews for movie {MovieId}", movieId);

                    var reviews = context.Reviews
                        .Where(r => r.MovieId == movieId)
                        .ToList();

                    transaction.Commit();
                    _logger.LogInformation("Successfully fetched {Count} reviews for movie {MovieId}", reviews.Count, movieId);

                    return reviews;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch reviews for movie {MovieId}", movieId);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    public List<Review> GetAllReviewsByUserId(int userId)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Fetching all reviews for user {UserId}", userId);

                    var reviews = context.Reviews
                        .Where(r => r.UserId == userId)
                        .ToList();

                    transaction.Commit();
                    _logger.LogInformation("Successfully fetched {Count} reviews for user {UserId}", reviews.Count, userId);

                    return reviews;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch reviews for user {UserId}", userId);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    public AverageRating GetAverageRatingByMovieId(int movieId)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Calculating average rating for movie {MovieId}", movieId);

                    var averageRating = context.Reviews
                        .Where(r => r.MovieId == movieId)
                        .Average(r => r.Rating);

                    transaction.Commit();
                    _logger.LogInformation("Successfully calculated average rating for movie {MovieId}: {AverageRating}", movieId, averageRating);

                    return new AverageRating
                    {
                        MovieId = movieId,
                        AverageRatings = averageRating
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to calculate average rating for movie {MovieId}", movieId);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}