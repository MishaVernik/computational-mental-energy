using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmeSim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainedModelTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BestParameters",
                table: "TrainingJobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActiveModel",
                table: "TrainingJobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingJobs_IsActiveModel",
                table: "TrainingJobs",
                column: "IsActiveModel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingJobs_IsActiveModel",
                table: "TrainingJobs");

            migrationBuilder.DropColumn(
                name: "BestParameters",
                table: "TrainingJobs");

            migrationBuilder.DropColumn(
                name: "IsActiveModel",
                table: "TrainingJobs");
        }
    }
}

