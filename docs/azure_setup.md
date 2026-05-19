# Azure Setup — CME Digital Twin (Phase 1)

Reproducible provisioning of the Azure resources needed for the hybrid digital twin:
**Azure Digital Twins** (DTDL ontology + twin instances), **Azure Blob Storage**
(hosts `head_with_muse.glb` for 3D Scenes Studio), and an **Entra ID app registration**
(Service Principal that the local `CmeSim.Api` uses to push thin summary updates).

The whole runbook fits inside the $200 Azure free trial and stays well under $5/month
afterwards for the lab-only usage profile (see
[digital_twin_platform.md](digital_twin_platform.md) §Cost envelope).

## Prerequisites

- Azure subscription (free trial works).
- Azure CLI ≥ 2.60 installed and signed in: `az login`.
- ADT extension: `az extension add --name azure-iot --upgrade` (provides `az dt`).
- PowerShell 7+ (recommended) or any shell that supports `az`.

## One-shot provisioning

```powershell
cd <repo-root>
.\scripts\Provision-Azure.ps1 `
  -ResourceGroup "cme-dt-rg" `
  -Location "westeurope" `
  -AdtName "cme-dt-<initials>" `
  -StorageAccount "cmedtblob<initials>" `
  -SpName "cme-dt-sp"
```

The script is idempotent: re-running it after a partial failure picks up where it left
off. On success it prints the values you need to drop into
`CmeSim.Api/appsettings.json` (or, preferably, user-secrets / environment variables):

```json
{
  "AzureDigitalTwins": {
    "Endpoint": "https://cme-dt-<initials>.api.weu.digitaltwins.azure.net",
    "TenantId": "<tenant-guid>",
    "ClientId": "<sp-app-id>",
    "ClientSecret": "<sp-secret>"
  }
}
```

## What the script does (step-by-step, for the runbook)

1. **Resource group**

   ```powershell
   az group create --name "cme-dt-rg" --location "westeurope"
   ```

2. **Azure Digital Twins instance**

   ```powershell
   az dt create --dt-name "cme-dt-<initials>" --resource-group "cme-dt-rg" --location "westeurope"
   ```

3. **DTDL models** (uploads all 5 from `docs/dtdl/`)

   ```powershell
   az dt model create --dt-name "cme-dt-<initials>" --models `
     docs/dtdl/User.json `
     docs/dtdl/Headband.json `
     docs/dtdl/Electrode.json `
     docs/dtdl/Session.json `
     docs/dtdl/Window.json
   ```

4. **Storage account + container + GLB upload**

   ```powershell
   az storage account create --name "cmedtblob<initials>" --resource-group "cme-dt-rg" `
     --location "westeurope" --sku Standard_LRS --kind StorageV2
   az storage container create --name "twin-assets" `
     --account-name "cmedtblob<initials>" --auth-mode login --public-access blob
   az storage blob upload --account-name "cmedtblob<initials>" `
     --container-name "twin-assets" --name "head_with_muse.glb" `
     --file "cme-live-dashboard/public/head_with_muse.glb" --auth-mode login --overwrite
   ```

5. **Entra ID Service Principal + RBAC**

   ```powershell
   $sp = az ad sp create-for-rbac --name "cme-dt-sp" --skip-assignment | ConvertFrom-Json
   $adtId = az dt show --dt-name "cme-dt-<initials>" --query id -o tsv
   az role assignment create --assignee $sp.appId --role "Azure Digital Twins Data Owner" --scope $adtId
   ```

6. **3D Scenes Studio storage role** — your own user needs `Storage Blob Data Contributor`
   on the container so 3D Scenes Studio can read/write its scene config blob:

   ```powershell
   $userOid = az ad signed-in-user show --query id -o tsv
   $stId = az storage account show --name "cmedtblob<initials>" --resource-group "cme-dt-rg" --query id -o tsv
   az role assignment create --assignee $userOid --role "Storage Blob Data Contributor" --scope $stId
   ```

## Tear-down

```powershell
az group delete --name "cme-dt-rg" --yes --no-wait
```

Drops the whole stack; cost goes to zero.

## Cost monitoring

```powershell
az consumption usage list --start-date 2026-05-01 --end-date 2026-05-31 `
  --query "[?contains(instanceName, 'cme-dt')].{date:usageStart, svc:meterCategory, qty:usageQuantity, cost:pretaxCost}" `
  -o table
```

Target: total < **$1 per week** for the lab-only profile (1 user, ~30 min demo/week).
If the bill is higher, almost certainly the `IDigitalTwinSyncService` is firing too
often — verify it is in `Summary` mode (only `currentPFlow`, `currentCmeRateVnPerSec`,
`cumulativeCmeVn` updated every 30 s).

## Secret hygiene

- The Service Principal secret is *not* committed. Store it via
  `dotnet user-secrets set "AzureDigitalTwins:ClientSecret" "<value>"` for local dev.
- For CI / deployment, use environment variables or Key Vault references.
- If a secret leaks, rotate immediately:
  `az ad sp credential reset --id <sp-app-id>`.

## Verification checklist

- [ ] `az dt model list --dt-name <name>` returns 5 models with `dtmi:cme:*;1` identifiers.
- [ ] `az dt twin list --dt-name <name>` returns the 6 bootstrapper-created twins after
      `CmeSim.Api` first run (User, Headband, 4 Electrodes).
- [ ] `cmedtblob<initials>/twin-assets/head_with_muse.glb` is fetchable with the
      blob's anonymous read URL.
- [ ] Azure DT 3D Scenes Studio at <https://explorer.digitaltwins.azure.net/3dscenes>
      can attach to the ADT instance and bind the GLB.
