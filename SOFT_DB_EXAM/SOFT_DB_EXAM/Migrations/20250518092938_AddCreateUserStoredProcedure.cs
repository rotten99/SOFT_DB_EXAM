using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOFT_DB_EXAM.Migrations
{
    public partial class AddCreateUserStoredProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.sp_CreateUser
    @Username        NVARCHAR(256),
    @Password        NVARCHAR(256),
    @Email           NVARCHAR(256),
    @CreatedAt       DATETIME2,
    @MoviesReviewed  INT,
    @MoviesWatched   INT,
    @NewId           INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [Users] 
        (UserName, Password, Email, CreatedAt, MoviesReviewed, MoviesWatched)
    VALUES 
        (@Username, @Password, @Email, @CreatedAt, @MoviesReviewed, @MoviesWatched);

    SET @NewId = SCOPE_IDENTITY();
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the proc if it exists
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.sp_CreateUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateUser;
");
        }
    }
}