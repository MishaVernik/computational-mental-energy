using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CmeSim.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalGenerations = table.Column<int>(type: "int", nullable: false),
                    CompletedGenerations = table.Column<int>(type: "int", nullable: false),
                    BestFitness = table.Column<double>(type: "float", nullable: true),
                    TotalQpuCalls = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CmeWindowResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WindowId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CmeValue = table.Column<double>(type: "float", nullable: false),
                    PFlow = table.Column<double>(type: "float", nullable: false),
                    ShotsUsed = table.Column<int>(type: "int", nullable: false),
                    Depth = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmeWindowResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CmeWindowResults_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InferenceRequestLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WindowId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalLatencyMs = table.Column<int>(type: "int", nullable: false),
                    QpuLatencyMs = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InferenceRequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InferenceRequestLogs_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Sessions",
                columns: new[] { "Id", "EndedAt", "StartedAt", "UserId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), null, new DateTime(2025, 11, 19, 15, 13, 0, 0, DateTimeKind.Utc), "user001" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), null, new DateTime(2025, 11, 19, 16, 13, 0, 0, DateTimeKind.Utc), "user002" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CmeWindowResults_ComputedAt",
                table: "CmeWindowResults",
                column: "ComputedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CmeWindowResults_SessionId",
                table: "CmeWindowResults",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_InferenceRequestLogs_RequestedAt",
                table: "InferenceRequestLogs",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InferenceRequestLogs_SessionId",
                table: "InferenceRequestLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingJobs_CreatedAt",
                table: "TrainingJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingJobs_Status",
                table: "TrainingJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CmeWindowResults");

            migrationBuilder.DropTable(
                name: "InferenceRequestLogs");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "TrainingJobs");
        }
    }
}


