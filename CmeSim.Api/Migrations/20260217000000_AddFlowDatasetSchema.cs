using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmeSim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFlowDatasetSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'FlowDataset') EXEC('CREATE SCHEMA [FlowDataset]');");

            migrationBuilder.AddColumn<bool>(
                name: "FlowLabel",
                table: "CmeWindowResults",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FlowProbability",
                table: "CmeWindowResults",
                type: "float",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActionSpikes",
                schema: "FlowDataset",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionSpikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionSpikes_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EegWindowFeatures",
                schema: "FlowDataset",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionSpikeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WindowId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Delta_TP9 = table.Column<double>(type: "float", nullable: false),
                    Theta_TP9 = table.Column<double>(type: "float", nullable: false),
                    Alpha_TP9 = table.Column<double>(type: "float", nullable: false),
                    Beta_TP9 = table.Column<double>(type: "float", nullable: false),
                    Gamma_TP9 = table.Column<double>(type: "float", nullable: false),
                    Delta_AF7 = table.Column<double>(type: "float", nullable: false),
                    Theta_AF7 = table.Column<double>(type: "float", nullable: false),
                    Alpha_AF7 = table.Column<double>(type: "float", nullable: false),
                    Beta_AF7 = table.Column<double>(type: "float", nullable: false),
                    Gamma_AF7 = table.Column<double>(type: "float", nullable: false),
                    Delta_AF8 = table.Column<double>(type: "float", nullable: false),
                    Theta_AF8 = table.Column<double>(type: "float", nullable: false),
                    Alpha_AF8 = table.Column<double>(type: "float", nullable: false),
                    Beta_AF8 = table.Column<double>(type: "float", nullable: false),
                    Gamma_AF8 = table.Column<double>(type: "float", nullable: false),
                    Delta_TP10 = table.Column<double>(type: "float", nullable: false),
                    Theta_TP10 = table.Column<double>(type: "float", nullable: false),
                    Alpha_TP10 = table.Column<double>(type: "float", nullable: false),
                    Beta_TP10 = table.Column<double>(type: "float", nullable: false),
                    Gamma_TP10 = table.Column<double>(type: "float", nullable: false),
                    TaskDifficulty = table.Column<double>(type: "float", nullable: false),
                    Quality = table.Column<double>(type: "float", nullable: false),
                    FlowLabel = table.Column<bool>(type: "bit", nullable: true),
                    FlowProbability = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EegWindowFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EegWindowFeatures_ActionSpikes_ActionSpikeId",
                        column: x => x.ActionSpikeId,
                        principalSchema: "FlowDataset",
                        principalTable: "ActionSpikes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EegWindowFeatures_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionSpikes_SessionId",
                schema: "FlowDataset",
                table: "ActionSpikes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionSpikes_StartTime",
                schema: "FlowDataset",
                table: "ActionSpikes",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_ActionSpikes_EndTime",
                schema: "FlowDataset",
                table: "ActionSpikes",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_EegWindowFeatures_SessionId",
                schema: "FlowDataset",
                table: "EegWindowFeatures",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EegWindowFeatures_Timestamp",
                schema: "FlowDataset",
                table: "EegWindowFeatures",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EegWindowFeatures_FlowLabel",
                schema: "FlowDataset",
                table: "EegWindowFeatures",
                column: "FlowLabel");

            migrationBuilder.CreateIndex(
                name: "IX_EegWindowFeatures_ActionSpikeId",
                schema: "FlowDataset",
                table: "EegWindowFeatures",
                column: "ActionSpikeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EegWindowFeatures",
                schema: "FlowDataset");

            migrationBuilder.DropTable(
                name: "ActionSpikes",
                schema: "FlowDataset");

            migrationBuilder.Sql("DROP SCHEMA IF EXISTS [FlowDataset];");

            migrationBuilder.DropColumn(
                name: "FlowLabel",
                table: "CmeWindowResults");

            migrationBuilder.DropColumn(
                name: "FlowProbability",
                table: "CmeWindowResults");
        }
    }
}
