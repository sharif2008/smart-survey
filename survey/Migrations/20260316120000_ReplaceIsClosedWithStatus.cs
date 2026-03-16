using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyApi.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsClosedWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Surveys",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Migrate: IsClosed 1 (true) -> Status -1 (closed), 0 (false) -> Status 1 (active)
            migrationBuilder.Sql("UPDATE Surveys SET Status = IF(IsClosed = 1, -1, 1)");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "Surveys");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "Surveys",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE Surveys SET IsClosed = CASE WHEN Status = -1 THEN 1 ELSE 0 END");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Surveys");
        }
    }
}
