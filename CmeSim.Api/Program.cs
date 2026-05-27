using CmeSim.Api.Data;
using CmeSim.Api.Hubs;
using CmeSim.Api.Services;
using CmeSim.Api.Services.Pipelines;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// User-secrets are loaded by default only in Development; force-load them in any
// environment so AzureDigitalTwins:Endpoint/TenantId/ClientSecret are picked up
// from `dotnet user-secrets` no matter what ASPNETCORE_ENVIRONMENT is.
builder.Configuration.AddUserSecrets<Program>(optional: true);

// Configure request size limits for large CSV uploads (e.g., Mind Monitor files)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10_485_760; // 10MB
});

// Configure Kestrel server options for request body size limit and timeouts
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10_485_760; // 10MB
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10); // 10 minutes
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10); // 10 minutes
});

// Add services to the container
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Allow longer processing times for large file uploads
        options.SuppressModelStateInvalidFilter = false;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CME Simulation API",
        Version = "v1",
        Description = "Imitation model of quantum machine learning web application for EEG-based mental state detection"
    });
});

// Database -- SQL Server (connection string from config)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required");
builder.Services.AddDbContext<CmeSimDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContextFactory<CmeSimDbContext>(options => options.UseSqlServer(connectionString));

// HTTP Clients
builder.Services.AddHttpClient<IQuantumBackendClient, QuantumBackendHttpClient>(client =>
{
    var baseUrl = builder.Configuration.GetValue<string>("QuantumBackend:BaseUrl") ?? "http://localhost:8001";
    client.BaseAddress = new Uri(baseUrl);
    
    // Set timeout to 24 hours (86400 seconds) for large file processing (effectively unlimited)
    var timeoutSeconds = builder.Configuration.GetValue<int>("QuantumBackend:TimeoutSeconds", 86400);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

// Flow classifier (classical NN) HTTP client
builder.Services.AddHttpClient<IFlowClassifierClient, FlowClassifierHttpClient>(client =>
{
    var baseUrl = builder.Configuration.GetValue<string>("FlowClassifier:BaseUrl") ?? "http://localhost:8002";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("FlowClassifier:TimeoutSeconds", 30));
});

// PreprocessService HTTP client (for Architecture B) - calls back to this same API
builder.Services.AddHttpClient("PreprocessService", client =>
{
    // In Docker, this calls the API internally; in dev, localhost:5000
    var baseUrl = builder.Configuration.GetValue<string>("PreprocessService:BaseUrl") ?? "http://localhost:5000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(10); // 10 minutes for long-running requests
});

// Application Services
builder.Services.AddSingleton<ICmeCalculator, CmeCalculator>();
builder.Services.AddSingleton<IDerivedMetricsService, DerivedMetricsService>();
builder.Services.AddScoped<IExperimentMetricsService, ExperimentMetricsService>();
builder.Services.AddScoped<ICmeMetricsService, CmeMetricsService>();
builder.Services.AddScoped<PreprocessService>();
builder.Services.AddScoped<IBenchmarkRunnerService, BenchmarkRunnerService>();

// Pipeline implementations
builder.Services.AddScoped<MonolithPipeline>();
builder.Services.AddScoped<SyncMicroservicesPipeline>();
builder.Services.AddScoped<BrokeredPipeline>();

// Broker (in-memory, can be replaced with Redis/RabbitMQ later)
builder.Services.AddSingleton<IBrokerQueue, InMemoryBrokerQueue>();

// Dataset writer (async FlowDataset writes)
builder.Services.AddSingleton<DatasetWriterService>();
builder.Services.AddSingleton<IDatasetWriterService>(sp => sp.GetRequiredService<DatasetWriterService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<DatasetWriterService>());

// Background Services
builder.Services.AddHostedService<TrainingWorkerService>();
builder.Services.AddHostedService<BrokerWorkerService>();

// Azure Digital Twins mirror (no-op fallback when Endpoint is empty, so the local
// pipeline is identical with or without an Azure subscription).
builder.Services.Configure<AzureDigitalTwinsOptions>(
    builder.Configuration.GetSection(AzureDigitalTwinsOptions.SectionName));
var adtOpts = builder.Configuration
    .GetSection(AzureDigitalTwinsOptions.SectionName)
    .Get<AzureDigitalTwinsOptions>() ?? new AzureDigitalTwinsOptions();
if (adtOpts.IsEnabled)
{
    builder.Services.AddSingleton<IDigitalTwinSyncService, DigitalTwinSyncService>();
    builder.Services.AddHostedService<DigitalTwinBootstrapper>();
}
else
{
    builder.Services.AddSingleton<IDigitalTwinSyncService, NoOpDigitalTwinSyncService>();
}

// SignalR for real-time EEG streaming
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100KB
})
.AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// CORS (allow simulation client and live dashboard to call API)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001",
                           "http://192.168.1.10:3001", "http://192.168.1.10:3000",
                           "http://161.97.146.52:3001", "http://161.97.146.52:5000",
                           "https://cmeflow.entertainmentpl.com",
                           "http://cmeflow.entertainmentpl.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    // Fallback for non-SignalR clients that don't send credentials
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<EegStreamHub>("/eeg-stream");

// Schema is applied via Scripts/CreateSchema.sql (no EF migrations)

app.Run();

