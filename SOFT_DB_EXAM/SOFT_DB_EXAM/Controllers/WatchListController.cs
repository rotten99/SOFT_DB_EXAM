using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOFT_DB_EXAM.Entities;
using SOFT_DB_EXAM.Facades;

namespace SOFT_DB_EXAM.Controllers;

[ApiController]
[Route("api/watchlists")]
public class WatchListsController : ControllerBase
{
    private readonly WatchListFacade _watchListFacade;
    private readonly ILogger<WatchListsController> _logger;

    public WatchListsController(WatchListFacade watchListFacade, ILogger<WatchListsController> logger)
    {
        _watchListFacade = watchListFacade;
        _logger = logger;
    }

    [HttpPost("create")]
    [Authorize]
    public IActionResult CreateWatchList([FromQuery] string name, [FromQuery] bool isPrivate, [FromQuery] int userId)
    {
        var id = _watchListFacade.CreateWatchList(name, isPrivate, userId);
        return Ok(new { WatchListId = id });
    }

    [HttpPost("{watchListId}/add-movies")]
    [Authorize]
    public async Task<IActionResult> AddMoviesToWatchList(int watchListId, [FromBody] List<int> movieIds)
    {
        await _watchListFacade.AddMoviesToWatchListAsync(watchListId, movieIds);
        return Ok("Movies added.");
    }

    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var lists = await _watchListFacade.GetWatchListsByUserIdAsync(userId);
        return Ok(lists);
    }

    [HttpGet("{watchListId}")]
    [Authorize]
    public async Task<IActionResult> GetById(int watchListId)
    {
        var list = await _watchListFacade.GetWatchListByIdAsync(watchListId);
        return list == null ? NotFound() : Ok(list);
    }

    [HttpPost("{watchListId}/follow")]
    [Authorize]
    public async Task<IActionResult> Follow(int watchListId, [FromQuery] int userId)
    {
        await _watchListFacade.FollowWatchListAsync(userId, watchListId);
        return Ok("Followed.");
    }

    [HttpPost("{watchListId}/unfollow")]
    [Authorize]
    public async Task<IActionResult> Unfollow(int watchListId, [FromQuery] int userId)
    {
        await _watchListFacade.UnfollowWatchListAsync(userId, watchListId);
        return Ok("Unfollowed.");
    }

    [HttpGet("followed/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetFollowed(int userId)
    {
        var lists = await _watchListFacade.GetFollowedWatchListsByUserIdAsync(userId);
        return Ok(lists);
    }
    
    [HttpPost("{watchListId}/remove-movie")]
    [Authorize]
    public async Task<IActionResult> RemoveMovie(int watchListId, [FromQuery] int movieId)
    {
        await _watchListFacade.RemoveMovieFromWatchListAsync(watchListId, movieId);
        return Ok("Movie removed.");
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
        => Ok(await _watchListFacade.GetAllWatchListsAsync());


}
