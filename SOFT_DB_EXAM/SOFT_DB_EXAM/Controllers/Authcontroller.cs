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
        // 1) Look up the user
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.UserName == model.Username);

        // 2) If not found or password doesn’t match, reject
        if (user == null || !BCrypt.Verify(model.Password, user.Password))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // 3) Create claims (you could include user.Id if you like)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim("uid", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // 4) Generate signing credentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 5) Build the token
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpireMinutes),
            signingCredentials: creds
        );

        // 6) Return the serialized token and expiry
        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiration = token.ValidTo
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
        // optional: check for existing username/email
        if (await _dbContext.Users
                .AnyAsync(u => u.UserName == model.Username || u.Email == model.Email))
        {
            return Conflict(new { message = "Username or email already in use" });
        }

        // hash the password
        var hashed = BCrypt.HashPassword(model.Password);

        // get the underlying ADO connection/command
        var conn = _dbContext.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_CreateUser";

        // add input parameters
        cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = model.Username });
        cmd.Parameters.Add(new SqlParameter("@Password", SqlDbType.NVarChar, 60) { Value = hashed });
        cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = model.Email });
        cmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = DateTime.UtcNow });
        cmd.Parameters.Add(new SqlParameter("@MoviesReviewed", SqlDbType.Int) { Value = 0 });
        cmd.Parameters.Add(new SqlParameter("@MoviesWatched", SqlDbType.Int) { Value = 0 });

        // add output parameter
        var outId = new SqlParameter("@NewId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(outId);

        // execute the proc
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await cmd.ExecuteNonQueryAsync();

        // read the new ID
        var newUserId = (int)outId.Value!;

        // return 201 Created with the new ID
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
}