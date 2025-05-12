
using Microsoft.EntityFrameworkCore;

namespace SOFT_DB_EXAM.Facades;

public class UserFacade 
{
    private ILogger<UserFacade> _logger;
    
    public UserFacade(ILogger<UserFacade> logger)
    {
        _logger = logger;
    }
    
    public void CreateUser(string username, string password, string email)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Creating user {Username}", username);

                    var user = new User
                    {
                        UserName = username,
                        Password = password,
                        Email = email
                    };

                    context.Users.Add(user);
                    context.SaveChanges();

                    transaction.Commit();
                    _logger.LogInformation("Successfully created user {Username}", username);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create user {Username}", username);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    public User GetUserById(int userId)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Fetching user with ID {UserId}", userId);

                    var user = context.Users
                        .Include(u => u.Reviews)
                        .Include(u => u.WatchListsOwned)
                        .Include(u => u.WatchListsFollowed)
                        .Include(u => u.FavouriteMovies)
                        .FirstOrDefault(u => u.Id == userId);

                    transaction.Commit();
                    _logger.LogInformation("Successfully fetched user with ID {UserId}", userId);

                    return user;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch user with ID {UserId}", userId);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    public void UpdateUser(User user)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Updating user with ID {UserId}", user.Id);

                    context.Users.Update(user);
                    context.SaveChanges();

                    transaction.Commit();
                    _logger.LogInformation("Successfully updated user with ID {UserId}", user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update user with ID {UserId}", user.Id);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    public void DeleteUser(int userId)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Deleting user with ID {UserId}", userId);

                    var user = context.Users.Find(userId);
                    if (user != null)
                    {
                        context.Users.Remove(user);
                        context.SaveChanges();
                    }

                    transaction.Commit();
                    _logger.LogInformation("Successfully deleted user with ID {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete user with ID {UserId}", userId);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    public List<User> GetAllUsers()
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Fetching all users");

                    var users = context.Users
                        .Include(u => u.Reviews)
                        .Include(u => u.WatchListsOwned)
                        .Include(u => u.WatchListsFollowed)
                        .Include(u => u.FavouriteMovies)
                        .ToList();

                    transaction.Commit();
                    _logger.LogInformation("Successfully fetched {Count} users", users.Count);

                    return users;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch all users");
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    public User GetUserByUsername(string username)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Fetching user with username {Username}", username);

                    var user = context.Users
                        .Include(u => u.Reviews)
                        .Include(u => u.WatchListsOwned)
                        .Include(u => u.WatchListsFollowed)
                        .Include(u => u.FavouriteMovies)
                        .FirstOrDefault(u => u.UserName == username);

                    transaction.Commit();
                    _logger.LogInformation("Successfully fetched user with username {Username}", username);

                    return user;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch user with username {Username}", username);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    public User GetUserByEmail(string email)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Fetching user with email {Email}", email);

                    var user = context.Users
                        .Include(u => u.Reviews)
                        .Include(u => u.WatchListsOwned)
                        .Include(u => u.WatchListsFollowed)
                        .Include(u => u.FavouriteMovies)
                        .FirstOrDefault(u => u.Email == email);

                    transaction.Commit();
                    _logger.LogInformation("Successfully fetched user with email {Email}", email);

                    return user;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch user with email {Email}", email);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    public User Login(string username, string password)
    {
        using (var context = ApplicationContextFactory.CreateDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    _logger.LogInformation("Logging in user {Username}", username);

                    var user = context.Users
                        .FirstOrDefault(u => u.UserName == username && u.Password == password);

                    transaction.Commit();
                    _logger.LogInformation("Successfully logged in user {Username}", username);

                    return user;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to log in user {Username}", username);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    
    
}