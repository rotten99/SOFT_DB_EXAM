using Microsoft.AspNetCore.Mvc;
using SOFT_DB_EXAM.Facades;

[ApiController]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    private readonly MovieFacade _movieFacade;

    public MoviesController(MovieFacade movieFacade)
    {
        _movieFacade = movieFacade;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var movie = await _movieFacade.GetByIdAsync(id);
        return movie == null ? NotFound() : Ok(movie);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var movies = await _movieFacade.GetAllAsync(page, pageSize);
        return Ok(movies);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _movieFacade.SearchByTitleAsync(q, page, pageSize);
        return Ok(result);
    }
    
    [HttpGet("smart-search")]
    public async Task<IActionResult> SmartSearch([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var results = await _movieFacade.SearchByKeywordsAsync(q, page, pageSize);
        return Ok(results);
    }
    
    [HttpPost("by-ids")]
    public async Task<IActionResult> GetByIds([FromBody] List<int> ids)
    {
        if (ids == null || !ids.Any())
            return BadRequest("No movie IDs provided.");

        var movies = await _movieFacade.GetByIdsAsync(ids);
        return Ok(movies);
    }


}