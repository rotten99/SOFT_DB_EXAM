using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SOFT_DB_EXAM;
using SOFT_DB_EXAM.Facades;
using SOFT_DB_EXAM.Hubs;
using StackExchange.Redis;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;


public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

// SQL Server (EF Core)
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// MongoDB
        builder.Services.Configure<MongoDbSettings>(
            builder.Configuration.GetSection("MongoDbSettings"));

        builder.Services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        builder.Services.AddScoped<MovieFacade, MovieFacade>();
        builder.Services.AddScoped<RatingSeederService>();


// Redis
        builder.Services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]));
        builder.Services.AddScoped<RedisFacade>();
        builder.Services.AddScoped<ReviewFacade>();
        builder.Services.AddScoped<WatchListFacade>();
        builder.Services.AddScoped<UserFacade>();
        builder.Services.AddScoped<FavouriteMovieFacade>();
        builder.Services.AddSignalR();
        builder.Services.AddScoped<WatchPartyFacade>();

        builder.Services.AddControllers();


        builder.Services.AddLogging();

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(c =>
        {
            // 1) The normal Swagger setup
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });

            // 2) Add JWT bearer support
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.ApiKey,
                Scheme       = "Bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "Enter: Bearer {your JWT token here}"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme 
                    { 
                        Reference = new OpenApiReference 
                        { 
                            Type = ReferenceType.SecurityScheme, 
                            Id   = "Bearer" 
                        } 
                    },
                    Array.Empty<string>()
                }
            });
        });

        
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetIsOriginAllowed(_ => true); // Accept all origins for testing
            });
        });
        
        builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection("Jwt"));

// 2) Add Authentication & JWT Bearer
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var jwtSettings = jwtSection.Get<JwtSettings>()!;

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer   = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key!)),

                    ClockSkew = TimeSpan.Zero
                };
            });

// 3) Add Authorization
        builder.Services.AddAuthorization();


        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Apply any pending migrations and create the database if it doesn't exist
                await dbContext.Database.MigrateAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var ratingSeeder = scope.ServiceProvider.GetRequiredService<RatingSeederService>();
            await ratingSeeder.SeedAverageRatingsIfEmptyAsync();
        }
        



// Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        
        app.UseDefaultFiles(); // Serves index.html if no file is specified
        app.UseStaticFiles();  // Allows serving .html, .js, .css etc. from wwwroot/

        app.UseCors("AllowAll");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHub<WatchPartyHub>("/hub/watchparty"); 
        });

        app.Run();
    }
}