using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOFT_DB_EXAM.Dtos;
using SOFT_DB_EXAM.Entities;
using SOFT_DB_EXAM.Facades;

namespace SOFT_DB_EXAM.Controllers;

[ApiController]
[Route("api/watchparties")]
public class WatchPartyController : ControllerBase
{
    private readonly WatchPartyFacade _facade;
    private readonly ILogger<WatchPartyController> _logger;

    public WatchPartyController(WatchPartyFacade facade, ILogger<WatchPartyController> logger)
    {
        _facade = facade;
        _logger = logger;
    }
    
    [HttpGet("active")]
    [Authorize]
    public async Task<ActionResult<List<WatchParty>>> GetActive()
    {
        var list = await _facade.GetActiveWatchPartiesAsync();
        return Ok(list);
    }

    [HttpGet("upcoming")]
    [Authorize]
    public async Task<ActionResult<List<WatchParty>>> GetUpcoming()
    {
        var list = await _facade.GetUpcomingWatchPartiesAsync();
        return Ok(list);
    }
    
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _facade.GetByIdAsync(id);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateWatchPartyDto dto)
    {
        var id = await _facade.CreateWatchParty(dto.Title, dto.MovieIds, dto.UserIds, dto.StartTime, dto.EndTime);
        return Ok(new { PartyId = id });
    } 
    [HttpPost("subscribe")]
    [Authorize]
    public async Task<IActionResult> Subscribe([FromBody] WatchPartySubscriptionDto dto)
    {
        try
        {
            var success = await _facade.SubscribeUserAsync(dto.PartyId, dto.UserId);

            // If already subscribed, still return success
            if (!success)
            {
                _logger.LogInformation("User {UserId} was already subscribed to WatchParty {PartyId}", dto.UserId, dto.PartyId);
            }

            return Ok("User is subscribed (or already was).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing user {UserId} to party {PartyId}", dto.UserId, dto.PartyId);
            return StatusCode(500, "An error occurred.");
        }
    }

    
    [HttpPost("join")]
    [Authorize]
    public async Task<IActionResult> Join([FromBody] WatchPartySubscriptionDto dto)
    {
        using var context = ApplicationContextFactory.CreateDbContext();

        var party = await context.WatchParties.FindAsync(dto.PartyId);
        var user = await context.Users.FindAsync(dto.UserId);

        if (party == null || user == null)
            return NotFound("Party or user not found.");

        return Ok(user.UserName); // Skip checking if user is subscribed
    }

    
}

public class WatchPartySubscriptionDto
{
    public int PartyId { get; set; }
    public int UserId { get; set; }
}