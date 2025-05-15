using Microsoft.AspNetCore.Mvc;
using SOFT_DB_EXAM.Facades;

[ApiController]
[Route("api/favourites")]
public class FavouriteMoviesController : ControllerBase
{
    private readonly FavouriteMovieFacade _facade;

    public FavouriteMoviesController(FavouriteMovieFacade facade)
    {
        _facade = facade;
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromQuery] int userId, [FromQuery] int movieId)
    {
        await _facade.AddFavouriteAsync(userId, movieId);
        return Ok("Added to favourites.");
    }

    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromQuery] int userId, [FromQuery] int movieId)
    {
        await _facade.RemoveFavouriteAsync(userId, movieId);
        return Ok("Removed from favourites.");
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetAll(int userId)
    {
        var favourites = await _facade.GetFavouritesByUserIdAsync(userId);
        return Ok(favourites);
    }
}