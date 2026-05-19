# Azure Credentials — When and How

Short answer to "why hasn't the system asked me for an Azure token yet?":

**By design.** The local lab demo runs with **zero Azure dependencies**.
The repo opts you in to Azure Digital Twins only when you explicitly choose to.
This file explains the exact opt-in path and the four secrets you'll need.

## Default state (after this lab work) = local-only

Look at [`CmeSim.Api/appsettings.json`](../CmeSim.Api/appsettings.json):

```json
"AzureDigitalTwins": {
  "Endpoint": "",
  "TenantId": "",
  "ClientId": "",
  "ClientSecret": "",
  ...
}
```

`Endpoint` is empty. The DI wiring in
[`CmeSim.Api/Program.cs`](../CmeSim.Api/Program.cs) treats that as the off
switch:

```csharp
if (adtOpts.IsEnabled)
{
    builder.Services.AddSingleton<IDigitalTwinSyncService, DigitalTwinSyncService>();
    builder.Services.AddHostedService<DigitalTwinBootstrapper>();
}
else
{
    builder.Services.AddSingleton<IDigitalTwinSyncService, NoOpDigitalTwinSyncService>();
}
```

Consequence: starting the API right now, with no Azure account at all, logs one
line —

```
AzureDigitalTwins:Endpoint is empty - digital twin sync is disabled (local-only mode).
```

— and the SignalR → CME → dashboard → Three.js avatar path runs identically.
**Nothing prompts you for a token because nothing is calling Azure.** That
keeps the lab demo (and the video) safe from any cloud, network, or auth
failure.

## When the prompts (well, the secret-setting) actually happen

Three sequential decisions, in order:

1. **Do you want Azure DT screenshots in the lab report?**
   - If **no**: stop here. The report's §8 platform-choice section still
     contains the DTDL ontology, the cost analysis, the 3D Scenes Studio
     config, and a Mermaid diagram — that's enough to evidence the platform
     decision. Submission-grade.
   - If **yes**: continue.

2. **Run the provisioning script.**

   ```powershell
   .\scripts\Provision-Azure.ps1 `
     -AdtName "cme-dt-<initials>" `
     -StorageAccount "cmedtblob<initials>"
   ```

   This is the **first** time you'll be asked for credentials, and it is
   handled by the Azure CLI (`az login`) — i.e. a browser pop-up against
   your Microsoft account, not the app. The script:
   - Creates the resource group + ADT instance + storage account.
   - Uploads the 5 DTDL files.
   - Uploads `head_with_muse.glb` to Blob Storage.
   - Creates a **Service Principal** named `cme-dt-sp` and prints its four
     secrets to your terminal.

3. **Drop the four secrets into local user-secrets.**

   The provisioning script ends by printing a JSON block like:

   ```json
   {
     "AzureDigitalTwins": {
       "Endpoint":     "https://cme-dt-xx.api.weu.digitaltwins.azure.net",
       "TenantId":     "00000000-0000-0000-0000-000000000000",
       "ClientId":     "11111111-1111-1111-1111-111111111111",
       "ClientSecret": "rotated-secret-here"
     }
   }
   ```

   Set these via **user-secrets**, never commit them:

   ```powershell
   cd CmeSim.Api
   dotnet user-secrets init
   dotnet user-secrets set "AzureDigitalTwins:Endpoint"     "https://cme-dt-xx.api.weu.digitaltwins.azure.net"
   dotnet user-secrets set "AzureDigitalTwins:TenantId"     "00000000-0000-0000-0000-000000000000"
   dotnet user-secrets set "AzureDigitalTwins:ClientId"     "11111111-1111-1111-1111-111111111111"
   dotnet user-secrets set "AzureDigitalTwins:ClientSecret" "rotated-secret-here"
   ```

   Confirm:

   ```powershell
   dotnet user-secrets list
   ```

   Now restart `CmeSim.Api`. You'll see, in order:

   ```
   DigitalTwinSyncService active: endpoint=https://cme-dt-xx.api.weu.digitaltwins.azure.net, interval=30s, diffOnly=True
   Base twins ensured (User + Headband + 4 Electrodes + relationships).
   ```

## What the secrets are, and what they let in

| Secret | What it is | Scope |
|---|---|---|
| `Endpoint` | Your ADT instance's public HTTPS URL. | Not secret; could be put in `appsettings.Development.json` if you like. |
| `TenantId` | Your Microsoft Entra tenant GUID. | Not secret. |
| `ClientId` | The Service Principal's app id. | Not secret. |
| **`ClientSecret`** | The Service Principal's password. | **Secret.** Grants `Azure Digital Twins Data Owner` on the ADT instance. Anyone who has it can read and write any twin. |

Only `ClientSecret` is truly sensitive. Treat it like a database password.

## Rotation, expiry, and emergency revoke

The CLI default secret lifetime is **2 years**. To rotate before then:

```powershell
az ad sp credential reset --id <client-id>
```

To revoke the SP entirely (and shut down all ADT writes from your local API
immediately):

```powershell
az ad sp delete --id <client-id>
```

To shut everything down at the Azure side:

```powershell
az group delete --name cme-dt-rg --yes --no-wait
```

That stops billing and removes all twins; the local stack is unaffected.

## Common confusion points

| Question | Answer |
|---|---|
| "The Provision-Azure.ps1 script needs my password — is that the ClientSecret?" | No. That's your **Microsoft account** sign-in for the Azure CLI (`az login`). Done once per session, in a browser. The script then creates the SP secret automatically. |
| "Can I use `DefaultAzureCredential` and skip the SP secret?" | Yes, for local dev. If you've done `az login`, `DigitalTwinSyncService` will pick up your CLI credentials when `ClientId/ClientSecret` are empty (`DefaultAzureCredential` is the fallback in [`DigitalTwinSyncService.BuildCredential`](../CmeSim.Api/Services/DigitalTwinSyncService.cs)). The SP path exists for unattended scenarios (Docker, CI). |
| "I committed the ClientSecret by mistake." | (1) `az ad sp credential reset --id <client-id>` right now. (2) Force-rewrite git history with `git filter-repo` to scrub the value. (3) Add a pre-commit gitleaks hook. |
| "I want zero Azure for the lab demo." | Default state already gives you that. The `NoOp*` service is the active code path until `Endpoint` is set. |

## In one sentence

The Azure side is **opt-in**, gated by a single line in `appsettings.json`
(`AzureDigitalTwins.Endpoint`), and the only secret you'll handle is one
Service Principal password produced by `Provision-Azure.ps1` and stored in
user-secrets. Set nothing, and the lab demo still runs.
