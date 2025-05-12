using Microsoft.EntityFrameworkCore;
using SOFT_DB_EXAM.Entities;

namespace SOFT_DB_EXAM;

public class ApplicationDbContext : DbContext
{

    public DbSet<User> Users { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<WatchList> WatchLists { get; set; }
    public DbSet<FollowedWatchList> FollowedWatchLists { get; set; }
    public DbSet<FavouriteMovie> FavouriteMovies { get; set; }
    public DbSet<ListedMovie> ListedMovies { get; set; }
    public DbSet<AverageRating> AverageRatings { get; set; }
    
    
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public ApplicationDbContext()
    {
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure SQL Server if no options are provided (to avoid overriding options in tests)
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost,1433;Database=movieDB;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;");
        }
    }


}