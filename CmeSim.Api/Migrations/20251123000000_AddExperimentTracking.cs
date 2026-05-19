using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmeSim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExperimentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Experiments table
            migrationBuilder.CreateTable(
                name: "Experiments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    OnlineArrivalRate = table.Column<double>(type: "float", nullable: false),
                    NumberOfClients = table.Column<int>(type: "int", nullable: false),
                    TrainingArrivalRate = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiments", x => x.Id);
                });

            // Create QpuInvocationLogs table
            migrationBuilder.CreateTable(
                name: "QpuInvocationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExperimentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Shots = table.Column<int>(type: "int", nullable: false),
                    BackendName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QpuInvocationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QpuInvocationLogs_Experiments_ExperimentId",
                        column: x => x.ExperimentId,
                        principalTable: "Experiments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create ExperimentModelMetrics table
            migrationBuilder.CreateTable(
                name: "ExperimentModelMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExperimentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelAvgLatencyMs = table.Column<double>(type: "float", nullable: false),
                    ModelP95LatencyMs = table.Column<double>(type: "float", nullable: true),
                    ModelThroughputReqPerSec = table.Column<double>(type: "float", nullable: false),
                    ModelQpuUtilization = table.Column<double>(type: "float", nullable: false),
                    ModelAvgJobDurationSec = table.Column<double>(type: "float", nullable: true),
                    SavedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentModelMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExperimentModelMetrics_Experiments_ExperimentId",
                        column: x => x.ExperimentId,
                        principalTable: "Experiments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Add ExperimentId to InferenceRequestLogs
            migrationBuilder.AddColumn<Guid>(
                name: "ExperimentId",
                table: "InferenceRequestLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "InferenceRequestLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccess",
                table: "InferenceRequestLogs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorType",
                table: "InferenceRequestLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Add ExperimentId to TrainingJobs
            migrationBuilder.AddColumn<Guid>(
                name: "ExperimentId",
                table: "TrainingJobs",
                type: "uniqueidentifier",
                nullable: true);

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Experiments_StartedAt",
                table: "Experiments",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_Status",
                table: "Experiments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QpuInvocationLogs_ExperimentId",
                table: "QpuInvocationLogs",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_QpuInvocationLogs_StartedAt",
                table: "QpuInvocationLogs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InferenceRequestLogs_ExperimentId",
                table: "InferenceRequestLogs",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingJobs_ExperimentId",
                table: "TrainingJobs",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentModelMetrics_ExperimentId",
                table: "ExperimentModelMetrics",
                column: "ExperimentId",
                unique: true);

            // Add foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK_InferenceRequestLogs_Experiments_ExperimentId",
                table: "InferenceRequestLogs",
                column: "ExperimentId",
                principalTable: "Experiments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingJobs_Experiments_ExperimentId",
                table: "TrainingJobs",
                column: "ExperimentId",
                principalTable: "Experiments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_InferenceRequestLogs_Experiments_ExperimentId", table: "InferenceRequestLogs");
            migrationBuilder.DropForeignKey(name: "FK_TrainingJobs_Experiments_ExperimentId", table: "TrainingJobs");

            migrationBuilder.DropTable(name: "ExperimentModelMetrics");
            migrationBuilder.DropTable(name: "QpuInvocationLogs");
            migrationBuilder.DropTable(name: "Experiments");

            migrationBuilder.DropIndex(name: "IX_InferenceRequestLogs_ExperimentId", table: "InferenceRequestLogs");
            migrationBuilder.DropIndex(name: "IX_TrainingJobs_ExperimentId", table: "TrainingJobs");

            migrationBuilder.DropColumn(name: "ExperimentId", table: "InferenceRequestLogs");
            migrationBuilder.DropColumn(name: "FinishedAt", table: "InferenceRequestLogs");
            migrationBuilder.DropColumn(name: "IsSuccess", table: "InferenceRequestLogs");
            migrationBuilder.DropColumn(name: "ErrorType", table: "InferenceRequestLogs");
            migrationBuilder.DropColumn(name: "ExperimentId", table: "TrainingJobs");
        }
    }
}

