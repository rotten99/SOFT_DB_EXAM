using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOFT_DB_EXAM.Dtos;
using SOFT_DB_EXAM.Facades;
using SOFT_DB_EXAM.Entities;

namespace SOFT_DB_EXAM.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly ReviewFacade _reviewFacade;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(ReviewFacade reviewFacade, ILogger<ReviewsController> logger)
    {
        _reviewFacade = reviewFacade;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        try
        {
            await _reviewFacade.CreateReviewAsync(dto.ReviewText, dto.Rating, dto.MovieId, dto.UserId, dto.Title);
            return Ok("Review created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create review.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var reviews = await _reviewFacade.GetReviewsByUserIdAsync(userId);
        return Ok(reviews);
    }

    [HttpGet("movie/{movieId}")]
    [Authorize]
    public async Task<IActionResult> GetByMovieId(int movieId)
    {
        var reviews = await _reviewFacade.GetReviewsByMovieIdAsync(movieId);
        return Ok(reviews);
    }

    [HttpGet("movie/{movieId}/average-rating")]
    [Authorize]
    public async Task<IActionResult> GetAverageRating(int movieId)
    {
        var rating = await _reviewFacade.GetAverageRatingByMovieIdAsync(movieId);
        return rating == null ? NotFound() : Ok(rating);
    }
    
    [HttpGet("{reviewId}")]
    [Authorize]
    public async Task<IActionResult> GetById(int reviewId)
    {
        var review = await _reviewFacade.GetReviewByIdAsync(reviewId);
        return review == null ? NotFound() : Ok(review);
    }

}