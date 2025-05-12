using Microsoft.EntityFrameworkCore;
using SOFT_DB_EXAM.Entities;

namespace SOFT_DB_EXAM;

public class ApplicationDbContext : DbContext
{

    public DbSet<User> Users { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<WatchList> WatchLists { get; set; }
    public DbSet<WatchListsFollowed> WatchListsFollowed { get; set; }
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
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure WatchListsFollowed many-to-many join
        modelBuilder.Entity<WatchListsFollowed>()
            .HasKey(wf => wf.Id);

        modelBuilder.Entity<WatchListsFollowed>()
            .HasOne(wf => wf.User)
            .WithMany(u => u.WatchListsFollowed)
            .HasForeignKey(wf => wf.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path

        modelBuilder.Entity<WatchListsFollowed>()
            .HasOne(wf => wf.WatchList)
            .WithMany(wl => wl.Followers)
            .HasForeignKey(wf => wf.WatchListId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path

        // Optional: configure WatchLists -> User if you want this one to cascade
        modelBuilder.Entity<WatchList>()
            .HasOne(wl => wl.User)
            .WithMany(u => u.WatchListsOwned)
            .HasForeignKey(wl => wl.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }




}