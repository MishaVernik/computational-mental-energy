namespace CmeSim.Api.Services;

public class AzureDigitalTwinsOptions
{
    public const string SectionName = "AzureDigitalTwins";

    /// <summary>Full ADT endpoint, e.g. https://cme-dt-xx.api.weu.digitaltwins.azure.net. Empty disables sync.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Entra tenant id (optional if using DefaultAzureCredential with az login).</summary>
    public string? TenantId { get; set; }

    /// <summary>Service Principal app id (optional).</summary>
    public string? ClientId { get; set; }

    /// <summary>Service Principal secret (set via user-secrets or env var; do NOT commit).</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Minimum seconds between updates per twin id. Throttles cost at ~$0.01/Mops.</summary>
    public int SyncIntervalSeconds { get; set; } = 30;

    /// <summary>If true, only properties whose value changed are sent.</summary>
    public bool DiffOnly { get; set; } = true;

    /// <summary>Twin id of the (single) User instance.</summary>
    public string UserTwinId { get; set; } = "user-default";

    /// <summary>Twin id of the (single) Headband instance.</summary>
    public string HeadbandTwinId { get; set; } = "headband-default";

    public bool IsEnabled => !string.IsNullOrWhiteSpace(Endpoint);
}
