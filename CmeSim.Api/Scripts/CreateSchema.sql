-- cme Database Schema
-- Run this script against your Azure SQL database (e.g. bookbadge)
-- Uses [cme] schema to avoid conflicts with other projects in dbo
-- Idempotent: uses IF NOT EXISTS where possible

-- cme schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'cme')
    EXEC('CREATE SCHEMA [cme]');
GO

-- Sessions
IF OBJECT_ID(N'[cme].[Sessions]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[Sessions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(100) NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        CONSTRAINT [PK_Sessions] PRIMARY KEY ([Id])
    );
END;
GO

-- Experiments (required before TrainingJobs.ExperimentId FK)
IF OBJECT_ID(N'[cme].[Experiments]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[Experiments] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [FinishedAt] datetime2 NULL,
        [DurationSeconds] int NOT NULL,
        [OnlineArrivalRate] float NOT NULL,
        [NumberOfClients] int NOT NULL,
        [TrainingArrivalRate] float NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_Experiments] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_Experiments_StartedAt] ON [cme].[Experiments] ([StartedAt]);
    CREATE INDEX [IX_Experiments_Status] ON [cme].[Experiments] ([Status]);
END;
GO

-- TrainingJobs
IF OBJECT_ID(N'[cme].[TrainingJobs]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[TrainingJobs] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [Status] nvarchar(50) NOT NULL,
        [Algorithm] nvarchar(50) NOT NULL DEFAULT 'genetic',
        [TotalGenerations] int NOT NULL DEFAULT 0,
        [CompletedGenerations] int NOT NULL DEFAULT 0,
        [BestFitness] float NULL,
        [TotalQpuCalls] int NOT NULL DEFAULT 0,
        [ErrorMessage] nvarchar(max) NULL,
        [BestParameters] nvarchar(max) NULL,
        [IsActiveModel] bit NOT NULL DEFAULT 0,
        [ExperimentId] uniqueidentifier NULL,
        CONSTRAINT [PK_TrainingJobs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrainingJobs_Experiments_ExperimentId] FOREIGN KEY ([ExperimentId]) REFERENCES [cme].[Experiments] ([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_TrainingJobs_CreatedAt] ON [cme].[TrainingJobs] ([CreatedAt]);
    CREATE INDEX [IX_TrainingJobs_Status] ON [cme].[TrainingJobs] ([Status]);
    CREATE INDEX [IX_TrainingJobs_IsActiveModel] ON [cme].[TrainingJobs] ([IsActiveModel]);
    CREATE INDEX [IX_TrainingJobs_ExperimentId] ON [cme].[TrainingJobs] ([ExperimentId]);
END;
GO

-- Add ExperimentId to TrainingJobs if missing (for existing DBs)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[cme].[TrainingJobs]') AND name = 'ExperimentId')
BEGIN
    ALTER TABLE [cme].[TrainingJobs] ADD [ExperimentId] uniqueidentifier NULL;
    CREATE INDEX [IX_TrainingJobs_ExperimentId] ON [cme].[TrainingJobs] ([ExperimentId]);
    ALTER TABLE [cme].[TrainingJobs] ADD CONSTRAINT [FK_TrainingJobs_Experiments_ExperimentId]
        FOREIGN KEY ([ExperimentId]) REFERENCES [cme].[Experiments] ([Id]) ON DELETE SET NULL;
END;
GO

-- CmeWindowResults
IF OBJECT_ID(N'[cme].[CmeWindowResults]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[CmeWindowResults] (
        [Id] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [WindowId] nvarchar(100) NOT NULL,
        [ComputedAt] datetime2 NOT NULL,
        [CmeValue] float NOT NULL,
        [PFlow] float NOT NULL,
        [ShotsUsed] int NOT NULL,
        [Depth] int NOT NULL,
        [FlowLabel] bit NULL,
        [FlowProbability] float NULL,
        CONSTRAINT [PK_CmeWindowResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CmeWindowResults_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [cme].[Sessions] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_CmeWindowResults_SessionId] ON [cme].[CmeWindowResults] ([SessionId]);
    CREATE INDEX [IX_CmeWindowResults_ComputedAt] ON [cme].[CmeWindowResults] ([ComputedAt]);
END;
GO

-- Add FlowLabel, FlowProbability to CmeWindowResults if missing
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[cme].[CmeWindowResults]') AND name = 'FlowLabel')
    ALTER TABLE [cme].[CmeWindowResults] ADD [FlowLabel] bit NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[cme].[CmeWindowResults]') AND name = 'FlowProbability')
    ALTER TABLE [cme].[CmeWindowResults] ADD [FlowProbability] float NULL;
GO

-- InferenceRequestLogs
IF OBJECT_ID(N'[cme].[InferenceRequestLogs]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[InferenceRequestLogs] (
        [Id] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [WindowId] nvarchar(100) NOT NULL,
        [ExperimentId] uniqueidentifier NULL,
        [RequestedAt] datetime2 NOT NULL,
        [FinishedAt] datetime2 NULL,
        [TotalLatencyMs] int NOT NULL,
        [QpuLatencyMs] int NOT NULL,
        [IsSuccess] bit NOT NULL DEFAULT 1,
        [ErrorType] nvarchar(100) NULL,
        CONSTRAINT [PK_InferenceRequestLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InferenceRequestLogs_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [cme].[Sessions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InferenceRequestLogs_Experiments_ExperimentId] FOREIGN KEY ([ExperimentId]) REFERENCES [cme].[Experiments] ([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_InferenceRequestLogs_SessionId] ON [cme].[InferenceRequestLogs] ([SessionId]);
    CREATE INDEX [IX_InferenceRequestLogs_RequestedAt] ON [cme].[InferenceRequestLogs] ([RequestedAt]);
    CREATE INDEX [IX_InferenceRequestLogs_ExperimentId] ON [cme].[InferenceRequestLogs] ([ExperimentId]);
END;
GO

-- Add ExperimentId to InferenceRequestLogs if missing (for existing DBs)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[cme].[InferenceRequestLogs]') AND name = 'ExperimentId')
BEGIN
    ALTER TABLE [cme].[InferenceRequestLogs] ADD [ExperimentId] uniqueidentifier NULL;
    CREATE INDEX [IX_InferenceRequestLogs_ExperimentId] ON [cme].[InferenceRequestLogs] ([ExperimentId]);
    ALTER TABLE [cme].[InferenceRequestLogs] ADD CONSTRAINT [FK_InferenceRequestLogs_Experiments_ExperimentId]
        FOREIGN KEY ([ExperimentId]) REFERENCES [cme].[Experiments] ([Id]) ON DELETE SET NULL;
END;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[cme].[InferenceRequestLogs]') AND name = 'FinishedAt')
    ALTER TABLE [cme].[InferenceRequestLogs] ADD [FinishedAt] datetime2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[cme].[InferenceRequestLogs]') AND name = 'IsSuccess')
    ALTER TABLE [cme].[InferenceRequestLogs] ADD [IsSuccess] bit NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[cme].[InferenceRequestLogs]') AND name = 'ErrorType')
    ALTER TABLE [cme].[InferenceRequestLogs] ADD [ErrorType] nvarchar(100) NULL;
GO

-- QpuInvocationLogs
IF OBJECT_ID(N'[cme].[QpuInvocationLogs]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[QpuInvocationLogs] (
        [Id] uniqueidentifier NOT NULL,
        [ExperimentId] uniqueidentifier NULL,
        [StartedAt] datetime2 NOT NULL,
        [FinishedAt] datetime2 NOT NULL,
        [DurationMs] int NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Shots] int NOT NULL,
        [BackendName] nvarchar(50) NULL,
        CONSTRAINT [PK_QpuInvocationLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QpuInvocationLogs_Experiments_ExperimentId] FOREIGN KEY ([ExperimentId]) REFERENCES [cme].[Experiments] ([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_QpuInvocationLogs_ExperimentId] ON [cme].[QpuInvocationLogs] ([ExperimentId]);
    CREATE INDEX [IX_QpuInvocationLogs_StartedAt] ON [cme].[QpuInvocationLogs] ([StartedAt]);
END;
GO

-- ExperimentModelMetrics
IF OBJECT_ID(N'[cme].[ExperimentModelMetrics]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[ExperimentModelMetrics] (
        [Id] uniqueidentifier NOT NULL,
        [ExperimentId] uniqueidentifier NOT NULL,
        [ModelAvgLatencyMs] float NOT NULL,
        [ModelP95LatencyMs] float NULL,
        [ModelThroughputReqPerSec] float NOT NULL,
        [ModelQpuUtilization] float NOT NULL,
        [ModelAvgJobDurationSec] float NULL,
        [SavedAt] datetime2 NOT NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_ExperimentModelMetrics] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExperimentModelMetrics_Experiments_ExperimentId] FOREIGN KEY ([ExperimentId]) REFERENCES [cme].[Experiments] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_ExperimentModelMetrics_ExperimentId] ON [cme].[ExperimentModelMetrics] ([ExperimentId]);
END;
GO

-- BenchmarkRuns
IF OBJECT_ID(N'[cme].[BenchmarkRuns]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[BenchmarkRuns] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Architecture] int NOT NULL,
        [ConfigJson] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [AvgLatencyMs] float NOT NULL,
        [P95LatencyMs] float NOT NULL,
        [P99LatencyMs] float NOT NULL,
        [ThroughputRps] float NOT NULL,
        [FailRate] float NOT NULL,
        [SuccessCount] int NOT NULL,
        [FailCount] int NOT NULL,
        [AvgQpuQueueLen] float NOT NULL,
        [MaxQpuQueueLen] int NOT NULL,
        [AvgBrokerQueueLen] float NOT NULL,
        [MaxBrokerQueueLen] int NOT NULL,
        [ValidateMs] float NOT NULL,
        [EnqueueMs] float NOT NULL,
        [PreprocessMs] float NOT NULL,
        [QpuWaitMs] float NOT NULL,
        [QpuServiceMs] float NOT NULL,
        [DbWriteMs] float NOT NULL,
        [ResponseMs] float NOT NULL,
        [ValidateStdMs] float NOT NULL,
        [EnqueueStdMs] float NOT NULL,
        [PreprocessStdMs] float NOT NULL,
        [QpuWaitStdMs] float NOT NULL,
        [QpuServiceStdMs] float NOT NULL,
        [DbWriteStdMs] float NOT NULL,
        [ResponseStdMs] float NOT NULL,
        [MetricsJson] nvarchar(max) NULL,
        CONSTRAINT [PK_BenchmarkRuns] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_BenchmarkRuns_Status] ON [cme].[BenchmarkRuns] ([Status]);
    CREATE INDEX [IX_BenchmarkRuns_CreatedAt] ON [cme].[BenchmarkRuns] ([CreatedAt]);
END;
GO

-- BenchmarkEvents
IF OBJECT_ID(N'[cme].[BenchmarkEvents]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[BenchmarkEvents] (
        [Id] uniqueidentifier NOT NULL,
        [BenchmarkRunId] uniqueidentifier NOT NULL,
        [RequestId] uniqueidentifier NOT NULL,
        [EventType] int NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [DurationMs] float NOT NULL,
        [Metadata] nvarchar(max) NULL,
        CONSTRAINT [PK_BenchmarkEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BenchmarkEvents_BenchmarkRuns_BenchmarkRunId] FOREIGN KEY ([BenchmarkRunId]) REFERENCES [cme].[BenchmarkRuns] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_BenchmarkEvents_BenchmarkRunId] ON [cme].[BenchmarkEvents] ([BenchmarkRunId]);
    CREATE INDEX [IX_BenchmarkEvents_RequestId] ON [cme].[BenchmarkEvents] ([RequestId]);
END;
GO

-- cme.ActionSpikes (raw data actions)
IF OBJECT_ID(N'[cme].[ActionSpikes]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[ActionSpikes] (
        [Id] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NOT NULL,
        [ActionType] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ActionSpikes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ActionSpikes_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [cme].[Sessions] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ActionSpikes_SessionId] ON [cme].[ActionSpikes] ([SessionId]);
    CREATE INDEX [IX_ActionSpikes_StartTime] ON [cme].[ActionSpikes] ([StartTime]);
    CREATE INDEX [IX_ActionSpikes_EndTime] ON [cme].[ActionSpikes] ([EndTime]);
END;
GO

-- cme.EegWindowFeatures (raw EEG band powers per window - where DatasetWriterService writes)
IF OBJECT_ID(N'[cme].[EegWindowFeatures]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[EegWindowFeatures] (
        [Id] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [ActionSpikeId] uniqueidentifier NULL,
        [WindowId] nvarchar(100) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [Delta_TP9] float NOT NULL,
        [Theta_TP9] float NOT NULL,
        [Alpha_TP9] float NOT NULL,
        [Beta_TP9] float NOT NULL,
        [Gamma_TP9] float NOT NULL,
        [Delta_AF7] float NOT NULL,
        [Theta_AF7] float NOT NULL,
        [Alpha_AF7] float NOT NULL,
        [Beta_AF7] float NOT NULL,
        [Gamma_AF7] float NOT NULL,
        [Delta_AF8] float NOT NULL,
        [Theta_AF8] float NOT NULL,
        [Alpha_AF8] float NOT NULL,
        [Beta_AF8] float NOT NULL,
        [Gamma_AF8] float NOT NULL,
        [Delta_TP10] float NOT NULL,
        [Theta_TP10] float NOT NULL,
        [Alpha_TP10] float NOT NULL,
        [Beta_TP10] float NOT NULL,
        [Gamma_TP10] float NOT NULL,
        [TaskDifficulty] float NOT NULL,
        [Quality] float NOT NULL,
        [FlowLabel] bit NULL,
        [FlowProbability] float NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EegWindowFeatures] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EegWindowFeatures_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [cme].[Sessions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EegWindowFeatures_ActionSpikes_ActionSpikeId] FOREIGN KEY ([ActionSpikeId]) REFERENCES [cme].[ActionSpikes] ([Id])
    );
    CREATE INDEX [IX_EegWindowFeatures_SessionId] ON [cme].[EegWindowFeatures] ([SessionId]);
    CREATE INDEX [IX_EegWindowFeatures_Timestamp] ON [cme].[EegWindowFeatures] ([Timestamp]);
    CREATE INDEX [IX_EegWindowFeatures_FlowLabel] ON [cme].[EegWindowFeatures] ([FlowLabel]);
    CREATE INDEX [IX_EegWindowFeatures_ActionSpikeId] ON [cme].[EegWindowFeatures] ([ActionSpikeId]);
END;
GO

-- ActionDefinitions (hierarchical action catalog)
IF OBJECT_ID(N'[cme].[ActionDefinitions]', 'U') IS NULL
BEGIN
    CREATE TABLE [cme].[ActionDefinitions] (
        [Id] uniqueidentifier NOT NULL,
        [ParentId] uniqueidentifier NULL,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DefaultDifficulty] float NOT NULL DEFAULT 0.5,
        [Icon] nvarchar(50) NULL,
        [IsSystem] bit NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ActionDefinitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ActionDefinitions_ActionDefinitions_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [cme].[ActionDefinitions] ([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_ActionDefinitions_Slug] ON [cme].[ActionDefinitions] ([Slug]);
    CREATE INDEX [IX_ActionDefinitions_ParentId] ON [cme].[ActionDefinitions] ([ParentId]);
END;
GO

-- Add ActionDefinitionId to ActionSpikes if missing
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[cme].[ActionSpikes]') AND name = 'ActionDefinitionId')
BEGIN
    ALTER TABLE [cme].[ActionSpikes] ADD [ActionDefinitionId] uniqueidentifier NULL;
    ALTER TABLE [cme].[ActionSpikes] ADD CONSTRAINT [FK_ActionSpikes_ActionDefinitions_ActionDefinitionId]
        FOREIGN KEY ([ActionDefinitionId]) REFERENCES [cme].[ActionDefinitions] ([Id]) ON DELETE SET NULL;
    CREATE INDEX [IX_ActionSpikes_ActionDefinitionId] ON [cme].[ActionSpikes] ([ActionDefinitionId]);
END;
GO

-- Add ActionSpikeId to CmeWindowResults if missing
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[cme].[CmeWindowResults]') AND name = 'ActionSpikeId')
BEGIN
    ALTER TABLE [cme].[CmeWindowResults] ADD [ActionSpikeId] uniqueidentifier NULL;
    ALTER TABLE [cme].[CmeWindowResults] ADD CONSTRAINT [FK_CmeWindowResults_ActionSpikes_ActionSpikeId]
        FOREIGN KEY ([ActionSpikeId]) REFERENCES [cme].[ActionSpikes] ([Id]);
    CREATE INDEX [IX_CmeWindowResults_ActionSpikeId] ON [cme].[CmeWindowResults] ([ActionSpikeId]);
END;
GO

-- Seed ActionDefinitions (categories + actions) – idempotent via MERGE
-- Categories
IF NOT EXISTS (SELECT 1 FROM [cme].[ActionDefinitions] WHERE [Id] = 'C0000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO [cme].[ActionDefinitions] ([Id],[ParentId],[Name],[Slug],[DefaultDifficulty],[Icon],[IsSystem],[IsActive],[CreatedAt]) VALUES
    ('C0000000-0000-0000-0000-000000000001',NULL,'Work','work',0,'briefcase',1,1,'2026-01-01'),
    ('C0000000-0000-0000-0000-000000000002',NULL,'Study','study',0,'book',1,1,'2026-01-01'),
    ('C0000000-0000-0000-0000-000000000003',NULL,'Creative','creative',0,'palette',1,1,'2026-01-01'),
    ('C0000000-0000-0000-0000-000000000004',NULL,'Physical','physical',0,'dumbbell',1,1,'2026-01-01'),
    ('C0000000-0000-0000-0000-000000000005',NULL,'Social','social',0,'users',1,1,'2026-01-01'),
    ('C0000000-0000-0000-0000-000000000006',NULL,'Leisure','leisure',0,'gamepad',1,1,'2026-01-01'),
    ('C0000000-0000-0000-0000-000000000007',NULL,'Mindfulness','mindfulness',0,'brain',1,1,'2026-01-01'),
    ('C0000000-0000-0000-0000-000000000008',NULL,'Daily','daily',0,'coffee',1,1,'2026-01-01');
END;
GO

-- Actions (leaf nodes)
IF NOT EXISTS (SELECT 1 FROM [cme].[ActionDefinitions] WHERE [Id] = 'A0000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO [cme].[ActionDefinitions] ([Id],[ParentId],[Name],[Slug],[DefaultDifficulty],[Icon],[IsSystem],[IsActive],[CreatedAt]) VALUES
    -- Work
    ('A0000000-0000-0000-0000-000000000001','C0000000-0000-0000-0000-000000000001','Coding','coding',0.70,'code',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000002','C0000000-0000-0000-0000-000000000001','Code Review','code-review',0.60,'search',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000003','C0000000-0000-0000-0000-000000000001','Frontend Dev','frontend-dev',0.70,'layout',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000004','C0000000-0000-0000-0000-000000000001','Backend Dev','backend-dev',0.75,'server',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000005','C0000000-0000-0000-0000-000000000001','DevOps','devops',0.65,'cloud',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000006','C0000000-0000-0000-0000-000000000001','Debugging','debugging',0.80,'bug',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000007','C0000000-0000-0000-0000-000000000001','System Design','system-design',0.85,'sitemap',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000008','C0000000-0000-0000-0000-000000000001','Meetings','meetings',0.40,'video',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000009','C0000000-0000-0000-0000-000000000001','Email','email',0.30,'mail',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000010','C0000000-0000-0000-0000-000000000001','Documentation','documentation',0.50,'file-text',1,1,'2026-01-01'),
    -- Study
    ('A0000000-0000-0000-0000-000000000011','C0000000-0000-0000-0000-000000000002','Reading (Technical)','reading-technical',0.60,'book-open',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000012','C0000000-0000-0000-0000-000000000002','Reading (General)','reading-general',0.35,'bookmark',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000013','C0000000-0000-0000-0000-000000000002','Math / Problem Solving','math',0.90,'calculator',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000014','C0000000-0000-0000-0000-000000000002','Research','research',0.70,'microscope',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000015','C0000000-0000-0000-0000-000000000002','Note-Taking','note-taking',0.45,'edit',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000016','C0000000-0000-0000-0000-000000000002','Exam Prep','exam-prep',0.80,'clipboard',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000017','C0000000-0000-0000-0000-000000000002','Lecture / Webinar','lecture',0.35,'presentation',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000018','C0000000-0000-0000-0000-000000000002','Flashcards','flashcards',0.50,'layers',1,1,'2026-01-01'),
    -- Creative
    ('A0000000-0000-0000-0000-000000000019','C0000000-0000-0000-0000-000000000003','Writing (Essays)','writing-essays',0.60,'pen-tool',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000020','C0000000-0000-0000-0000-000000000003','Writing (Creative)','writing-creative',0.55,'feather',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000021','C0000000-0000-0000-0000-000000000003','Drawing / Sketching','drawing',0.50,'pencil',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000022','C0000000-0000-0000-0000-000000000003','Music Composition','music-composition',0.70,'music',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000023','C0000000-0000-0000-0000-000000000003','Music Practice','music-practice',0.55,'headphones',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000024','C0000000-0000-0000-0000-000000000003','Graphic Design','graphic-design',0.60,'image',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000025','C0000000-0000-0000-0000-000000000003','Video Editing','video-editing',0.65,'film',1,1,'2026-01-01'),
    -- Physical
    ('A0000000-0000-0000-0000-000000000026','C0000000-0000-0000-0000-000000000004','Exercise','exercise',0.30,'activity',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000027','C0000000-0000-0000-0000-000000000004','Walking','walking',0.15,'navigation',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000028','C0000000-0000-0000-0000-000000000004','Yoga','yoga',0.25,'heart',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000029','C0000000-0000-0000-0000-000000000004','Stretching','stretching',0.15,'move',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000030','C0000000-0000-0000-0000-000000000004','Sports','sports',0.35,'trophy',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000031','C0000000-0000-0000-0000-000000000004','Dance','dance',0.40,'zap',1,1,'2026-01-01'),
    -- Social
    ('A0000000-0000-0000-0000-000000000032','C0000000-0000-0000-0000-000000000005','Conversation','conversation',0.40,'message-circle',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000033','C0000000-0000-0000-0000-000000000005','Presentation','presentation',0.65,'monitor',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000034','C0000000-0000-0000-0000-000000000005','Teaching','teaching',0.60,'award',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000035','C0000000-0000-0000-0000-000000000005','Interview','interview',0.70,'mic',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000036','C0000000-0000-0000-0000-000000000005','Phone Call','phone-call',0.30,'phone',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000037','C0000000-0000-0000-0000-000000000005','Group Discussion','group-discussion',0.50,'users',1,1,'2026-01-01'),
    -- Leisure
    ('A0000000-0000-0000-0000-000000000038','C0000000-0000-0000-0000-000000000006','Gaming (Strategy)','gaming-strategy',0.60,'target',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000039','C0000000-0000-0000-0000-000000000006','Gaming (Action)','gaming-action',0.45,'crosshair',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000040','C0000000-0000-0000-0000-000000000006','Gaming (Cards/Board)','gaming-cards',0.40,'grid',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000041','C0000000-0000-0000-0000-000000000006','Watching Video','watching-video',0.15,'play',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000042','C0000000-0000-0000-0000-000000000006','Browsing','browsing',0.20,'globe',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000043','C0000000-0000-0000-0000-000000000006','Social Media','social-media',0.20,'share-2',1,1,'2026-01-01'),
    -- Mindfulness
    ('A0000000-0000-0000-0000-000000000044','C0000000-0000-0000-0000-000000000007','Meditation','meditation',0.10,'sunset',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000045','C0000000-0000-0000-0000-000000000007','Breathwork','breathwork',0.10,'wind',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000046','C0000000-0000-0000-0000-000000000007','Body Scan','body-scan',0.15,'eye',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000047','C0000000-0000-0000-0000-000000000007','Resting (Eyes Open)','resting-eyes-open',0.05,'eye',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000048','C0000000-0000-0000-0000-000000000007','Resting (Eyes Closed)','resting-eyes-closed',0.05,'moon',1,1,'2026-01-01'),
    -- Daily
    ('A0000000-0000-0000-0000-000000000049','C0000000-0000-0000-0000-000000000008','Eating','eating',0.10,'utensils',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000050','C0000000-0000-0000-0000-000000000008','Commuting','commuting',0.20,'train',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000051','C0000000-0000-0000-0000-000000000008','Cooking','cooking',0.30,'thermometer',1,1,'2026-01-01'),
    ('A0000000-0000-0000-0000-000000000052','C0000000-0000-0000-0000-000000000008','Cleaning','cleaning',0.15,'trash-2',1,1,'2026-01-01');
END;
GO

PRINT 'Schema creation complete.';
