using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CmeSim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddActionDefinitionsAndPerActionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cme");

            migrationBuilder.RenameTable(
                name: "TrainingJobs",
                newName: "TrainingJobs",
                newSchema: "cme");

            migrationBuilder.RenameTable(
                name: "Sessions",
                newName: "Sessions",
                newSchema: "cme");

            migrationBuilder.RenameTable(
                name: "InferenceRequestLogs",
                newName: "InferenceRequestLogs",
                newSchema: "cme");

            migrationBuilder.RenameTable(
                name: "EegWindowFeatures",
                schema: "FlowDataset",
                newName: "EegWindowFeatures",
                newSchema: "cme");

            migrationBuilder.RenameTable(
                name: "CmeWindowResults",
                newName: "CmeWindowResults",
                newSchema: "cme");

            migrationBuilder.RenameTable(
                name: "ActionSpikes",
                schema: "FlowDataset",
                newName: "ActionSpikes",
                newSchema: "cme");

            migrationBuilder.AddColumn<string>(
                name: "Algorithm",
                schema: "cme",
                table: "TrainingJobs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BestParameters",
                schema: "cme",
                table: "TrainingJobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExperimentId",
                schema: "cme",
                table: "TrainingJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActiveModel",
                schema: "cme",
                table: "TrainingJobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ErrorType",
                schema: "cme",
                table: "InferenceRequestLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExperimentId",
                schema: "cme",
                table: "InferenceRequestLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                schema: "cme",
                table: "InferenceRequestLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccess",
                schema: "cme",
                table: "InferenceRequestLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ActionSpikeId",
                schema: "cme",
                table: "CmeWindowResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActionDefinitionId",
                schema: "cme",
                table: "ActionSpikes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActionDefinitions",
                schema: "cme",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultDifficulty = table.Column<double>(type: "float", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionDefinitions_ActionDefinitions_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "cme",
                        principalTable: "ActionDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkRuns",
                schema: "cme",
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
                name: "Experiments",
                schema: "cme",
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

            migrationBuilder.CreateTable(
                name: "BenchmarkEvents",
                schema: "cme",
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
                        principalSchema: "cme",
                        principalTable: "BenchmarkRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentModelMetrics",
                schema: "cme",
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
                        principalSchema: "cme",
                        principalTable: "Experiments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QpuInvocationLogs",
                schema: "cme",
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
                        principalSchema: "cme",
                        principalTable: "Experiments",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "cme",
                table: "ActionDefinitions",
                columns: new[] { "Id", "CreatedAt", "DefaultDifficulty", "Description", "Icon", "IsActive", "IsSystem", "Name", "ParentId", "Slug" },
                values: new object[,]
                {
                    { new Guid("c0000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, null, "briefcase", true, true, "Work", null, "work" },
                    { new Guid("c0000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, null, "book", true, true, "Study", null, "study" },
                    { new Guid("c0000000-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, null, "palette", true, true, "Creative", null, "creative" },
                    { new Guid("c0000000-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, null, "dumbbell", true, true, "Physical", null, "physical" },
                    { new Guid("c0000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, null, "users", true, true, "Social", null, "social" },
                    { new Guid("c0000000-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, null, "gamepad", true, true, "Leisure", null, "leisure" },
                    { new Guid("c0000000-0000-0000-0000-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, null, "brain", true, true, "Mindfulness", null, "mindfulness" },
                    { new Guid("c0000000-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, null, "coffee", true, true, "Daily", null, "daily" }
                });

            migrationBuilder.UpdateData(
                schema: "cme",
                table: "Sessions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "StartedAt",
                value: new DateTime(2026, 3, 27, 13, 20, 49, 632, DateTimeKind.Utc).AddTicks(8136));

            migrationBuilder.UpdateData(
                schema: "cme",
                table: "Sessions",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "StartedAt",
                value: new DateTime(2026, 3, 27, 14, 20, 49, 632, DateTimeKind.Utc).AddTicks(8146));

            migrationBuilder.InsertData(
                schema: "cme",
                table: "ActionDefinitions",
                columns: new[] { "Id", "CreatedAt", "DefaultDifficulty", "Description", "Icon", "IsActive", "IsSystem", "Name", "ParentId", "Slug" },
                values: new object[,]
                {
                    { new Guid("a0000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.69999999999999996, null, "code", true, true, "Coding", new Guid("c0000000-0000-0000-0000-000000000001"), "coding" },
                    { new Guid("a0000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.59999999999999998, null, "search", true, true, "Code Review", new Guid("c0000000-0000-0000-0000-000000000001"), "code-review" },
                    { new Guid("a0000000-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.69999999999999996, null, "layout", true, true, "Frontend Dev", new Guid("c0000000-0000-0000-0000-000000000001"), "frontend-dev" },
                    { new Guid("a0000000-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.75, null, "server", true, true, "Backend Dev", new Guid("c0000000-0000-0000-0000-000000000001"), "backend-dev" },
                    { new Guid("a0000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.65000000000000002, null, "cloud", true, true, "DevOps", new Guid("c0000000-0000-0000-0000-000000000001"), "devops" },
                    { new Guid("a0000000-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.80000000000000004, null, "bug", true, true, "Debugging", new Guid("c0000000-0000-0000-0000-000000000001"), "debugging" },
                    { new Guid("a0000000-0000-0000-0000-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.84999999999999998, null, "sitemap", true, true, "System Design", new Guid("c0000000-0000-0000-0000-000000000001"), "system-design" },
                    { new Guid("a0000000-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.40000000000000002, null, "video", true, true, "Meetings", new Guid("c0000000-0000-0000-0000-000000000001"), "meetings" },
                    { new Guid("a0000000-0000-0000-0000-000000000009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.29999999999999999, null, "mail", true, true, "Email", new Guid("c0000000-0000-0000-0000-000000000001"), "email" },
                    { new Guid("a0000000-0000-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.5, null, "file-text", true, true, "Documentation", new Guid("c0000000-0000-0000-0000-000000000001"), "documentation" },
                    { new Guid("a0000000-0000-0000-0000-000000000011"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.59999999999999998, null, "book-open", true, true, "Reading (Technical)", new Guid("c0000000-0000-0000-0000-000000000002"), "reading-technical" },
                    { new Guid("a0000000-0000-0000-0000-000000000012"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.34999999999999998, null, "bookmark", true, true, "Reading (General)", new Guid("c0000000-0000-0000-0000-000000000002"), "reading-general" },
                    { new Guid("a0000000-0000-0000-0000-000000000013"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.90000000000000002, null, "calculator", true, true, "Math / Problem Solving", new Guid("c0000000-0000-0000-0000-000000000002"), "math" },
                    { new Guid("a0000000-0000-0000-0000-000000000014"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.69999999999999996, null, "microscope", true, true, "Research", new Guid("c0000000-0000-0000-0000-000000000002"), "research" },
                    { new Guid("a0000000-0000-0000-0000-000000000015"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.45000000000000001, null, "edit", true, true, "Note-Taking", new Guid("c0000000-0000-0000-0000-000000000002"), "note-taking" },
                    { new Guid("a0000000-0000-0000-0000-000000000016"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.80000000000000004, null, "clipboard", true, true, "Exam Prep", new Guid("c0000000-0000-0000-0000-000000000002"), "exam-prep" },
                    { new Guid("a0000000-0000-0000-0000-000000000017"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.34999999999999998, null, "presentation", true, true, "Lecture / Webinar", new Guid("c0000000-0000-0000-0000-000000000002"), "lecture" },
                    { new Guid("a0000000-0000-0000-0000-000000000018"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.5, null, "layers", true, true, "Flashcards", new Guid("c0000000-0000-0000-0000-000000000002"), "flashcards" },
                    { new Guid("a0000000-0000-0000-0000-000000000019"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.59999999999999998, null, "pen-tool", true, true, "Writing (Essays)", new Guid("c0000000-0000-0000-0000-000000000003"), "writing-essays" },
                    { new Guid("a0000000-0000-0000-0000-000000000020"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.55000000000000004, null, "feather", true, true, "Writing (Creative)", new Guid("c0000000-0000-0000-0000-000000000003"), "writing-creative" },
                    { new Guid("a0000000-0000-0000-0000-000000000021"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.5, null, "pencil", true, true, "Drawing / Sketching", new Guid("c0000000-0000-0000-0000-000000000003"), "drawing" },
                    { new Guid("a0000000-0000-0000-0000-000000000022"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.69999999999999996, null, "music", true, true, "Music Composition", new Guid("c0000000-0000-0000-0000-000000000003"), "music-composition" },
                    { new Guid("a0000000-0000-0000-0000-000000000023"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.55000000000000004, null, "headphones", true, true, "Music Practice", new Guid("c0000000-0000-0000-0000-000000000003"), "music-practice" },
                    { new Guid("a0000000-0000-0000-0000-000000000024"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.59999999999999998, null, "image", true, true, "Graphic Design", new Guid("c0000000-0000-0000-0000-000000000003"), "graphic-design" },
                    { new Guid("a0000000-0000-0000-0000-000000000025"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.65000000000000002, null, "film", true, true, "Video Editing", new Guid("c0000000-0000-0000-0000-000000000003"), "video-editing" },
                    { new Guid("a0000000-0000-0000-0000-000000000026"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.29999999999999999, null, "activity", true, true, "Exercise", new Guid("c0000000-0000-0000-0000-000000000004"), "exercise" },
                    { new Guid("a0000000-0000-0000-0000-000000000027"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.14999999999999999, null, "navigation", true, true, "Walking", new Guid("c0000000-0000-0000-0000-000000000004"), "walking" },
                    { new Guid("a0000000-0000-0000-0000-000000000028"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.25, null, "heart", true, true, "Yoga", new Guid("c0000000-0000-0000-0000-000000000004"), "yoga" },
                    { new Guid("a0000000-0000-0000-0000-000000000029"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.14999999999999999, null, "move", true, true, "Stretching", new Guid("c0000000-0000-0000-0000-000000000004"), "stretching" },
                    { new Guid("a0000000-0000-0000-0000-000000000030"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.34999999999999998, null, "trophy", true, true, "Sports", new Guid("c0000000-0000-0000-0000-000000000004"), "sports" },
                    { new Guid("a0000000-0000-0000-0000-000000000031"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.40000000000000002, null, "zap", true, true, "Dance", new Guid("c0000000-0000-0000-0000-000000000004"), "dance" },
                    { new Guid("a0000000-0000-0000-0000-000000000032"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.40000000000000002, null, "message-circle", true, true, "Conversation", new Guid("c0000000-0000-0000-0000-000000000005"), "conversation" },
                    { new Guid("a0000000-0000-0000-0000-000000000033"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.65000000000000002, null, "monitor", true, true, "Presentation", new Guid("c0000000-0000-0000-0000-000000000005"), "presentation" },
                    { new Guid("a0000000-0000-0000-0000-000000000034"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.59999999999999998, null, "award", true, true, "Teaching", new Guid("c0000000-0000-0000-0000-000000000005"), "teaching" },
                    { new Guid("a0000000-0000-0000-0000-000000000035"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.69999999999999996, null, "mic", true, true, "Interview", new Guid("c0000000-0000-0000-0000-000000000005"), "interview" },
                    { new Guid("a0000000-0000-0000-0000-000000000036"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.29999999999999999, null, "phone", true, true, "Phone Call", new Guid("c0000000-0000-0000-0000-000000000005"), "phone-call" },
                    { new Guid("a0000000-0000-0000-0000-000000000037"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.5, null, "users", true, true, "Group Discussion", new Guid("c0000000-0000-0000-0000-000000000005"), "group-discussion" },
                    { new Guid("a0000000-0000-0000-0000-000000000038"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.59999999999999998, null, "target", true, true, "Gaming (Strategy)", new Guid("c0000000-0000-0000-0000-000000000006"), "gaming-strategy" },
                    { new Guid("a0000000-0000-0000-0000-000000000039"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.45000000000000001, null, "crosshair", true, true, "Gaming (Action)", new Guid("c0000000-0000-0000-0000-000000000006"), "gaming-action" },
                    { new Guid("a0000000-0000-0000-0000-000000000040"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.40000000000000002, null, "grid", true, true, "Gaming (Cards/Board)", new Guid("c0000000-0000-0000-0000-000000000006"), "gaming-cards" },
                    { new Guid("a0000000-0000-0000-0000-000000000041"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.14999999999999999, null, "play", true, true, "Watching Video", new Guid("c0000000-0000-0000-0000-000000000006"), "watching-video" },
                    { new Guid("a0000000-0000-0000-0000-000000000042"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.20000000000000001, null, "globe", true, true, "Browsing", new Guid("c0000000-0000-0000-0000-000000000006"), "browsing" },
                    { new Guid("a0000000-0000-0000-0000-000000000043"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.20000000000000001, null, "share-2", true, true, "Social Media", new Guid("c0000000-0000-0000-0000-000000000006"), "social-media" },
                    { new Guid("a0000000-0000-0000-0000-000000000044"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.10000000000000001, null, "sunset", true, true, "Meditation", new Guid("c0000000-0000-0000-0000-000000000007"), "meditation" },
                    { new Guid("a0000000-0000-0000-0000-000000000045"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.10000000000000001, null, "wind", true, true, "Breathwork", new Guid("c0000000-0000-0000-0000-000000000007"), "breathwork" },
                    { new Guid("a0000000-0000-0000-0000-000000000046"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.14999999999999999, null, "eye", true, true, "Body Scan", new Guid("c0000000-0000-0000-0000-000000000007"), "body-scan" },
                    { new Guid("a0000000-0000-0000-0000-000000000047"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.050000000000000003, null, "eye", true, true, "Resting (Eyes Open)", new Guid("c0000000-0000-0000-0000-000000000007"), "resting-eyes-open" },
                    { new Guid("a0000000-0000-0000-0000-000000000048"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.050000000000000003, null, "moon", true, true, "Resting (Eyes Closed)", new Guid("c0000000-0000-0000-0000-000000000007"), "resting-eyes-closed" },
                    { new Guid("a0000000-0000-0000-0000-000000000049"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.10000000000000001, null, "utensils", true, true, "Eating", new Guid("c0000000-0000-0000-0000-000000000008"), "eating" },
                    { new Guid("a0000000-0000-0000-0000-000000000050"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.20000000000000001, null, "train", true, true, "Commuting", new Guid("c0000000-0000-0000-0000-000000000008"), "commuting" },
                    { new Guid("a0000000-0000-0000-0000-000000000051"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.29999999999999999, null, "thermometer", true, true, "Cooking", new Guid("c0000000-0000-0000-0000-000000000008"), "cooking" },
                    { new Guid("a0000000-0000-0000-0000-000000000052"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.14999999999999999, null, "trash-2", true, true, "Cleaning", new Guid("c0000000-0000-0000-0000-000000000008"), "cleaning" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingJobs_ExperimentId",
                schema: "cme",
                table: "TrainingJobs",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_InferenceRequestLogs_ExperimentId",
                schema: "cme",
                table: "InferenceRequestLogs",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_CmeWindowResults_ActionSpikeId",
                schema: "cme",
                table: "CmeWindowResults",
                column: "ActionSpikeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionSpikes_ActionDefinitionId",
                schema: "cme",
                table: "ActionSpikes",
                column: "ActionDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionDefinitions_ParentId",
                schema: "cme",
                table: "ActionDefinitions",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionDefinitions_Slug",
                schema: "cme",
                table: "ActionDefinitions",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkEvents_BenchmarkRunId",
                schema: "cme",
                table: "BenchmarkEvents",
                column: "BenchmarkRunId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkEvents_RequestId",
                schema: "cme",
                table: "BenchmarkEvents",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkRuns_CreatedAt",
                schema: "cme",
                table: "BenchmarkRuns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkRuns_Status",
                schema: "cme",
                table: "BenchmarkRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentModelMetrics_ExperimentId",
                schema: "cme",
                table: "ExperimentModelMetrics",
                column: "ExperimentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_StartedAt",
                schema: "cme",
                table: "Experiments",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_Status",
                schema: "cme",
                table: "Experiments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QpuInvocationLogs_ExperimentId",
                schema: "cme",
                table: "QpuInvocationLogs",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_QpuInvocationLogs_StartedAt",
                schema: "cme",
                table: "QpuInvocationLogs",
                column: "StartedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionSpikes_ActionDefinitions_ActionDefinitionId",
                schema: "cme",
                table: "ActionSpikes",
                column: "ActionDefinitionId",
                principalSchema: "cme",
                principalTable: "ActionDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CmeWindowResults_ActionSpikes_ActionSpikeId",
                schema: "cme",
                table: "CmeWindowResults",
                column: "ActionSpikeId",
                principalSchema: "cme",
                principalTable: "ActionSpikes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InferenceRequestLogs_Experiments_ExperimentId",
                schema: "cme",
                table: "InferenceRequestLogs",
                column: "ExperimentId",
                principalSchema: "cme",
                principalTable: "Experiments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingJobs_Experiments_ExperimentId",
                schema: "cme",
                table: "TrainingJobs",
                column: "ExperimentId",
                principalSchema: "cme",
                principalTable: "Experiments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionSpikes_ActionDefinitions_ActionDefinitionId",
                schema: "cme",
                table: "ActionSpikes");

            migrationBuilder.DropForeignKey(
                name: "FK_CmeWindowResults_ActionSpikes_ActionSpikeId",
                schema: "cme",
                table: "CmeWindowResults");

            migrationBuilder.DropForeignKey(
                name: "FK_InferenceRequestLogs_Experiments_ExperimentId",
                schema: "cme",
                table: "InferenceRequestLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingJobs_Experiments_ExperimentId",
                schema: "cme",
                table: "TrainingJobs");

            migrationBuilder.DropTable(
                name: "ActionDefinitions",
                schema: "cme");

            migrationBuilder.DropTable(
                name: "BenchmarkEvents",
                schema: "cme");

            migrationBuilder.DropTable(
                name: "ExperimentModelMetrics",
                schema: "cme");

            migrationBuilder.DropTable(
                name: "QpuInvocationLogs",
                schema: "cme");

            migrationBuilder.DropTable(
                name: "BenchmarkRuns",
                schema: "cme");

            migrationBuilder.DropTable(
                name: "Experiments",
                schema: "cme");

            migrationBuilder.DropIndex(
                name: "IX_TrainingJobs_ExperimentId",
                schema: "cme",
                table: "TrainingJobs");

            migrationBuilder.DropIndex(
                name: "IX_InferenceRequestLogs_ExperimentId",
                schema: "cme",
                table: "InferenceRequestLogs");

            migrationBuilder.DropIndex(
                name: "IX_CmeWindowResults_ActionSpikeId",
                schema: "cme",
                table: "CmeWindowResults");

            migrationBuilder.DropIndex(
                name: "IX_ActionSpikes_ActionDefinitionId",
                schema: "cme",
                table: "ActionSpikes");

            migrationBuilder.DropColumn(
                name: "Algorithm",
                schema: "cme",
                table: "TrainingJobs");

            migrationBuilder.DropColumn(
                name: "BestParameters",
                schema: "cme",
                table: "TrainingJobs");

            migrationBuilder.DropColumn(
                name: "ExperimentId",
                schema: "cme",
                table: "TrainingJobs");

            migrationBuilder.DropColumn(
                name: "IsActiveModel",
                schema: "cme",
                table: "TrainingJobs");

            migrationBuilder.DropColumn(
                name: "ErrorType",
                schema: "cme",
                table: "InferenceRequestLogs");

            migrationBuilder.DropColumn(
                name: "ExperimentId",
                schema: "cme",
                table: "InferenceRequestLogs");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                schema: "cme",
                table: "InferenceRequestLogs");

            migrationBuilder.DropColumn(
                name: "IsSuccess",
                schema: "cme",
                table: "InferenceRequestLogs");

            migrationBuilder.DropColumn(
                name: "ActionSpikeId",
                schema: "cme",
                table: "CmeWindowResults");

            migrationBuilder.DropColumn(
                name: "ActionDefinitionId",
                schema: "cme",
                table: "ActionSpikes");

            migrationBuilder.EnsureSchema(
                name: "FlowDataset");

            migrationBuilder.RenameTable(
                name: "TrainingJobs",
                schema: "cme",
                newName: "TrainingJobs");

            migrationBuilder.RenameTable(
                name: "Sessions",
                schema: "cme",
                newName: "Sessions");

            migrationBuilder.RenameTable(
                name: "InferenceRequestLogs",
                schema: "cme",
                newName: "InferenceRequestLogs");

            migrationBuilder.RenameTable(
                name: "EegWindowFeatures",
                schema: "cme",
                newName: "EegWindowFeatures",
                newSchema: "FlowDataset");

            migrationBuilder.RenameTable(
                name: "CmeWindowResults",
                schema: "cme",
                newName: "CmeWindowResults");

            migrationBuilder.RenameTable(
                name: "ActionSpikes",
                schema: "cme",
                newName: "ActionSpikes",
                newSchema: "FlowDataset");

            migrationBuilder.UpdateData(
                table: "Sessions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "StartedAt",
                value: new DateTime(2025, 11, 19, 15, 13, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Sessions",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "StartedAt",
                value: new DateTime(2025, 11, 19, 16, 13, 0, 0, DateTimeKind.Utc));
        }
    }
}
