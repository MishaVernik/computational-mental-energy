using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmeSim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBenchmarkTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenchmarkRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Architecture = table.Column<int>(type: "int", nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AvgLatencyMs = table.Column<double>(type: "float", nullable: false),
                    P95LatencyMs = table.Column<double>(type: "float", nullable: false),
                    P99LatencyMs = table.Column<double>(type: "float", nullable: false),
                    ThroughputRps = table.Column<double>(type: "float", nullable: false),
                    FailRate = table.Column<double>(type: "float", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailCount = table.Column<int>(type: "int", nullable: false),
                    AvgQpuQueueLen = table.Column<double>(type: "float", nullable: false),
                    MaxQpuQueueLen = table.Column<int>(type: "int", nullable: false),
                    AvgBrokerQueueLen = table.Column<double>(type: "float", nullable: false),
                    MaxBrokerQueueLen = table.Column<int>(type: "int", nullable: false),
                    ValidateMs = table.Column<double>(type: "float", nullable: false),
                    EnqueueMs = table.Column<double>(type: "float", nullable: false),
                    PreprocessMs = table.Column<double>(type: "float", nullable: false),
                    QpuWaitMs = table.Column<double>(type: "float", nullable: false),
                    QpuServiceMs = table.Column<double>(type: "float", nullable: false),
                    DbWriteMs = table.Column<double>(type: "float", nullable: false),
                    ResponseMs = table.Column<double>(type: "float", nullable: false),
                    ValidateStdMs = table.Column<double>(type: "float", nullable: false),
                    EnqueueStdMs = table.Column<double>(type: "float", nullable: false),
                    PreprocessStdMs = table.Column<double>(type: "float", nullable: false),
                    QpuWaitStdMs = table.Column<double>(type: "float", nullable: false),
                    QpuServiceStdMs = table.Column<double>(type: "float", nullable: false),
                    DbWriteStdMs = table.Column<double>(type: "float", nullable: false),
                    ResponseStdMs = table.Column<double>(type: "float", nullable: false),
                    MetricsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BenchmarkRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMs = table.Column<double>(type: "float", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkEvents_BenchmarkRuns_BenchmarkRunId",
                        column: x => x.BenchmarkRunId,
                        principalTable: "BenchmarkRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkRuns_Status",
                table: "BenchmarkRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkRuns_CreatedAt",
                table: "BenchmarkRuns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkEvents_BenchmarkRunId",
                table: "BenchmarkEvents",
                column: "BenchmarkRunId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkEvents_RequestId",
                table: "BenchmarkEvents",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenchmarkEvents");

            migrationBuilder.DropTable(
                name: "BenchmarkRuns");
        }
    }
}

