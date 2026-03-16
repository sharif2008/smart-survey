using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyDeletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Surveys",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Surveys");
        }
    }
}
