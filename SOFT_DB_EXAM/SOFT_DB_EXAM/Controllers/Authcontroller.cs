using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SOFT_DB_EXAM.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using BCrypt = BCrypt.Net.BCrypt;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtSettings _jwt;
    private readonly ApplicationDbContext _dbContext;

    public AuthController(IOptions<JwtSettings> jwtOptions,
        ApplicationDbContext dbContext)
    {
        _jwt = jwtOptions.Value;
        _dbContext = dbContext;
    }

    public class LoginModel
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.UserName == model.Username);

        if (user == null || !BCrypt.Verify(model.Password, user.Password))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim("uid", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpireMinutes),
            signingCredentials: creds
        );

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiration = token.ValidTo,
            userId = user.Id
        });
    }

    public class RegisterModel
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Email { get; set; } = "";
    }

    [HttpPost("createuser")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUser([FromBody] RegisterModel model)
    {
        if (await _dbContext.Users
                .AnyAsync(u => u.UserName == model.Username || u.Email == model.Email))
        {
            return Conflict(new { message = "Username or email already in use" });
        }

        var hashed = BCrypt.HashPassword(model.Password);

        var conn = _dbContext.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_CreateUser";

        cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = model.Username });
        cmd.Parameters.Add(new SqlParameter("@Password", SqlDbType.NVarChar, 60) { Value = hashed });
        cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = model.Email });
        cmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = DateTime.UtcNow });
        cmd.Parameters.Add(new SqlParameter("@MoviesReviewed", SqlDbType.Int) { Value = 0 });
        cmd.Parameters.Add(new SqlParameter("@MoviesWatched", SqlDbType.Int) { Value = 0 });

        var outId = new SqlParameter("@NewId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(outId);

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await cmd.ExecuteNonQueryAsync();

        var newUserId = (int)outId.Value!;

        return CreatedAtAction(
            nameof(CreateUser),
            new { id = newUserId },
            new
            {
                Id = newUserId,
                Username = model.Username,
                Email = model.Email,
                CreatedAt = DateTime.UtcNow
            }
        );
    }

    public class EditUserModel
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    [HttpPut("edituser")]
    [Authorize]
    public async Task<IActionResult> EditUser([FromBody] EditUserModel model)
    {
        var uidClaim = User.FindFirst("uid")?.Value;
        if (!int.TryParse(uidClaim, out var userId))
            return Unauthorized();

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (!string.IsNullOrWhiteSpace(model.Username) &&
            model.Username != user.UserName)
        {
            if (await _dbContext.Users
                    .AnyAsync(u => u.UserName == model.Username && u.Id != userId))
            {
                return Conflict(new { message = "Username already in use" });
            }

            user.UserName = model.Username;
        }

        if (!string.IsNullOrWhiteSpace(model.Email) &&
            model.Email != user.Email)
        {
            if (await _dbContext.Users
                    .AnyAsync(u => u.Email == model.Email && u.Id != userId))
            {
                return Conflict(new { message = "Email already in use" });
            }

            user.Email = model.Email;
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.Password = BCrypt.HashPassword(model.Password);
        }

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            MoviesReviewed = user.MoviesReviewed,
            MoviesWatched = user.MoviesWatched
        });
    }
    
    // in AuthController

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var uidClaim = User.FindFirst("uid")?.Value;
        if (!int.TryParse(uidClaim, out var userId))
            return Unauthorized();

        var user = await _dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new {
                u.Id,
                Username = u.UserName,
                u.Email,
                u.CreatedAt,
                u.MoviesReviewed,
                u.MoviesWatched
            })
            .SingleOrDefaultAsync();

        if (user == null) return NotFound();
        return Ok(user);
    }

}