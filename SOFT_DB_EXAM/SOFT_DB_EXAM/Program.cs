using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SOFT_DB_EXAM;
using SOFT_DB_EXAM.Facades;
using SOFT_DB_EXAM.Hubs;
using StackExchange.Redis;

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
        builder.Services.AddSwaggerGen();
        
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
        app.MapHub<WatchPartyHub>("/hub/watchparty");
        



// Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        
        app.UseDefaultFiles(); // Serves index.html if no file is specified
        app.UseStaticFiles();  // Allows serving .html, .js, .css etc. from wwwroot/

        app.UseCors("AllowAll");
        app.UseAuthorization();
        app.MapControllers();


        app.Run();
    }
}