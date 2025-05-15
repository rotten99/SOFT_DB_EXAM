using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOFT_DB_EXAM.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewTrigger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
    CREATE TRIGGER trg_UpdateRatingsAndUserStatsAfterReviewInsert
    ON Reviews
    AFTER INSERT
    AS
    BEGIN
        SET NOCOUNT ON;

        -- Update AverageRatings table
        UPDATE ar
        SET 
            ar.AverageRatings = 
                (ar.AverageRatings * ar.NumberOfRatings + i.TotalRating) 
                / (ar.NumberOfRatings + i.Count),
            ar.NumberOfRatings = ar.NumberOfRatings + i.Count
        FROM AverageRatings ar
        JOIN (
            SELECT 
                MovieId,
                COUNT(*) AS Count,
                SUM(Rating) AS TotalRating
            FROM inserted
            GROUP BY MovieId
        ) i ON ar.MovieId = i.MovieId;

        -- Update User table
        UPDATE u
        SET u.MoviesReviewed = u.MoviesReviewed + i.Count
        FROM Users u
        JOIN (
            SELECT 
                UserId,
                COUNT(*) AS Count
            FROM inserted
            GROUP BY UserId
        ) i ON u.Id = i.UserId;
    END
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_UpdateRatingsAndUserStatsAfterReviewInsert");
        }

    }
}
