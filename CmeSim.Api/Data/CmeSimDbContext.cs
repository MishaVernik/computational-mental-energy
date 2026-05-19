using Microsoft.EntityFrameworkCore;
using CmeSim.Api.Models;
using CmeSim.Api.Models.FlowDataset;

namespace CmeSim.Api.Data;

/// <summary>
/// Database context for CME simulation system.
/// </summary>
public class CmeSimDbContext : DbContext
{
    public CmeSimDbContext(DbContextOptions<CmeSimDbContext> options)
        : base(options)
    {
    }

    public DbSet<Session> Sessions { get; set; } = null!;
    public DbSet<ActionSpike> ActionSpikes { get; set; } = null!;
    public DbSet<EegWindowFeatures> EegWindowFeatures { get; set; } = null!;
    public DbSet<InferenceRequestLog> InferenceRequestLogs { get; set; } = null!;
    public DbSet<CmeWindowResult> CmeWindowResults { get; set; } = null!;
    public DbSet<TrainingJob> TrainingJobs { get; set; } = null!;
    public DbSet<Experiment> Experiments { get; set; } = null!;
    public DbSet<QpuInvocationLog> QpuInvocationLogs { get; set; } = null!;
    public DbSet<ExperimentModelMetrics> ExperimentModelMetrics { get; set; } = null!;
    public DbSet<BenchmarkRun> BenchmarkRuns { get; set; } = null!;
    public DbSet<BenchmarkEvent> BenchmarkEvents { get; set; } = null!;
    public DbSet<ActionDefinition> ActionDefinitions { get; set; } = null!;
    public DbSet<WaitlistSignup> WaitlistSignups { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use CmeSim schema to avoid conflicts with other projects sharing the database (dbo)
        modelBuilder.HasDefaultSchema("cme");

        // Configure indexes for performance
        modelBuilder.Entity<InferenceRequestLog>()
            .HasIndex(i => i.SessionId);

        modelBuilder.Entity<InferenceRequestLog>()
            .HasIndex(i => i.RequestedAt);

        modelBuilder.Entity<CmeWindowResult>()
            .HasIndex(c => c.SessionId);

        modelBuilder.Entity<CmeWindowResult>()
            .HasIndex(c => c.ComputedAt);

        modelBuilder.Entity<TrainingJob>()
            .HasIndex(t => t.Status);

        modelBuilder.Entity<TrainingJob>()
            .HasIndex(t => t.CreatedAt);

        modelBuilder.Entity<Experiment>()
            .HasIndex(e => e.StartedAt);

        modelBuilder.Entity<Experiment>()
            .HasIndex(e => e.Status);

        modelBuilder.Entity<QpuInvocationLog>()
            .HasIndex(q => q.ExperimentId);

        modelBuilder.Entity<QpuInvocationLog>()
            .HasIndex(q => q.StartedAt);

        modelBuilder.Entity<InferenceRequestLog>()
            .HasIndex(i => i.ExperimentId);

        modelBuilder.Entity<TrainingJob>()
            .HasIndex(t => t.ExperimentId);

        modelBuilder.Entity<BenchmarkRun>()
            .HasIndex(b => b.Status);

        modelBuilder.Entity<BenchmarkRun>()
            .HasIndex(b => b.CreatedAt);

        modelBuilder.Entity<BenchmarkEvent>()
            .HasIndex(e => e.BenchmarkRunId);

        modelBuilder.Entity<BenchmarkEvent>()
            .HasIndex(e => e.RequestId);

        // ActionSpikes and EegWindowFeatures in cme schema
        modelBuilder.Entity<ActionSpike>()
            .ToTable("ActionSpikes", "cme");
        modelBuilder.Entity<ActionSpike>()
            .HasIndex(a => a.SessionId);
        modelBuilder.Entity<ActionSpike>()
            .HasIndex(a => a.StartTime);
        modelBuilder.Entity<ActionSpike>()
            .HasIndex(a => a.EndTime);

        modelBuilder.Entity<EegWindowFeatures>()
            .ToTable("EegWindowFeatures", "cme");
        modelBuilder.Entity<EegWindowFeatures>()
            .HasIndex(e => e.SessionId);
        modelBuilder.Entity<EegWindowFeatures>()
            .HasIndex(e => e.Timestamp);
        modelBuilder.Entity<EegWindowFeatures>()
            .HasIndex(e => e.FlowLabel);
        modelBuilder.Entity<EegWindowFeatures>()
            .HasIndex(e => e.ActionSpikeId);

        // ActionDefinition hierarchy
        modelBuilder.Entity<ActionDefinition>()
            .ToTable("ActionDefinitions", "cme");
        modelBuilder.Entity<ActionDefinition>()
            .HasIndex(a => a.Slug).IsUnique();
        modelBuilder.Entity<ActionDefinition>()
            .HasIndex(a => a.ParentId);
        modelBuilder.Entity<ActionDefinition>()
            .HasOne(a => a.Parent)
            .WithMany(a => a.Children)
            .HasForeignKey(a => a.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ActionSpike → ActionDefinition FK
        modelBuilder.Entity<ActionSpike>()
            .HasOne(a => a.ActionDefinition)
            .WithMany()
            .HasForeignKey(a => a.ActionDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WaitlistSignup>()
            .ToTable("WaitlistSignups", "cme");
        modelBuilder.Entity<WaitlistSignup>()
            .HasIndex(w => w.Email).IsUnique();
        modelBuilder.Entity<WaitlistSignup>()
            .HasIndex(w => w.CreatedAt);

        ActionDefinitionSeed.Seed(modelBuilder);

        // Seed some initial sessions for testing
        modelBuilder.Entity<Session>().HasData(
            new Session
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId = "user001",
                StartedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Session
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                UserId = "user002",
                StartedAt = DateTime.UtcNow.AddHours(-1)
            }
        );
    }
}


