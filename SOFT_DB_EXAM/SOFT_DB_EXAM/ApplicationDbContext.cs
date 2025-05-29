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
    public DbSet<WatchParty> WatchParties { get; set; }

    
    
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public ApplicationDbContext()
    {
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("your-connection-string", opts =>
            {
                opts.MaxBatchSize(1);
            });
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

        modelBuilder.Entity<WatchList>()
            .HasOne(wl => wl.User)
            .WithMany(u => u.WatchListsOwned)
            .HasForeignKey(wl => wl.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }




}