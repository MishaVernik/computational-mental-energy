using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmeSim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAlgorithmToTrainingJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Algorithm",
                table: "TrainingJobs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "genetic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Algorithm",
                table: "TrainingJobs");
        }
    }
}

