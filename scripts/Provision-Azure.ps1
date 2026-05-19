<#
.SYNOPSIS
  Idempotently provisions the Azure resources for the CME Digital Twin (Phase 1).
.DESCRIPTION
  Creates: resource group, Azure Digital Twins instance + DTDL model uploads,
  Storage account + container + GLB upload, Entra Service Principal + RBAC.
  Outputs the JSON snippet to drop into CmeSim.Api configuration.

  Safe to re-run; all `az` commands are tolerant of pre-existing resources.

.PARAMETER ResourceGroup
  Resource group name. Defaults to "cme-dt-rg".
.PARAMETER Location
  Azure region. Defaults to "westeurope" (GDPR-friendly for EEG data).
.PARAMETER AdtName
  Globally-unique ADT instance name.
.PARAMETER StorageAccount
  Globally-unique storage account name (3-24 lowercase alphanumeric).
.PARAMETER SpName
  Display name for the Entra Service Principal.

.EXAMPLE
  ./scripts/Provision-Azure.ps1 -AdtName "cme-dt-mv" -StorageAccount "cmedtblobmv"
#>

param(
  [string]$ResourceGroup  = "cme-dt-rg",
  [string]$Location       = "westeurope",
  [Parameter(Mandatory)] [string]$AdtName,
  [Parameter(Mandatory)] [string]$StorageAccount,
  [string]$SpName         = "cme-dt-sp"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path "$PSScriptRoot\.."

function Section($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Ok($msg)      { Write-Host "  + $msg"      -ForegroundColor Green }

Section "Azure CLI version"
az version --query '"azure-cli"' -o tsv

Section "Resource group"
az group create --name $ResourceGroup --location $Location --only-show-errors | Out-Null
Ok "$ResourceGroup ($Location) ready"

Section "Azure Digital Twins"
$existingAdt = az dt show --dt-name $AdtName --resource-group $ResourceGroup --query name -o tsv 2>$null
if (-not $existingAdt) {
  az dt create --dt-name $AdtName --resource-group $ResourceGroup --location $Location --only-show-errors | Out-Null
  Ok "ADT instance $AdtName created"
} else {
  Ok "ADT instance $AdtName already exists"
}
$adtHostname = az dt show --dt-name $AdtName --resource-group $ResourceGroup --query "hostName" -o tsv
$adtEndpoint = "https://$adtHostname"
$adtId       = az dt show --dt-name $AdtName --resource-group $ResourceGroup --query id -o tsv

Section "DTDL model upload"
$models = @(
  "$repoRoot\docs\dtdl\User.json",
  "$repoRoot\docs\dtdl\Headband.json",
  "$repoRoot\docs\dtdl\Electrode.json",
  "$repoRoot\docs\dtdl\Session.json",
  "$repoRoot\docs\dtdl\Window.json"
)
$existingDtmis = (az dt model list --dt-name $AdtName --query "[].id" -o tsv) -split "`n"
if (-not ($existingDtmis -contains "dtmi:cme:User;1")) {
  az dt model create --dt-name $AdtName --models @models | Out-Null
  Ok "5 DTDL models uploaded"
} else {
  Ok "DTDL models already present"
}

Section "Storage account + GLB upload"
$existingSt = az storage account show --name $StorageAccount --resource-group $ResourceGroup --query name -o tsv 2>$null
if (-not $existingSt) {
  az storage account create `
    --name $StorageAccount --resource-group $ResourceGroup --location $Location `
    --sku Standard_LRS --kind StorageV2 --only-show-errors | Out-Null
  Ok "Storage account $StorageAccount created"
} else {
  Ok "Storage account $StorageAccount already exists"
}
$stId = az storage account show --name $StorageAccount --resource-group $ResourceGroup --query id -o tsv

# Grant self the Blob Data Contributor role so subsequent uploads use AAD auth.
$userOid = az ad signed-in-user show --query id -o tsv
$existingAssign = az role assignment list --assignee $userOid --scope $stId `
  --role "Storage Blob Data Contributor" --query "[0].id" -o tsv 2>$null
if (-not $existingAssign) {
  az role assignment create --assignee $userOid --role "Storage Blob Data Contributor" `
    --scope $stId --only-show-errors | Out-Null
  Ok "Granted current user Blob Data Contributor on $StorageAccount"
}

# Brief wait for RBAC propagation.
Start-Sleep -Seconds 15

az storage container create --name "twin-assets" --account-name $StorageAccount `
  --auth-mode login --public-access blob --only-show-errors | Out-Null
Ok "Container twin-assets ready (public blob read)"

$glb = "$repoRoot\cme-live-dashboard\public\head_with_muse.glb"
if (-not (Test-Path $glb)) { throw "GLB not found at $glb. Run 'npm run build:glb' in cme-live-dashboard first." }

az storage blob upload --account-name $StorageAccount --container-name "twin-assets" `
  --name "head_with_muse.glb" --file $glb --auth-mode login --overwrite --only-show-errors | Out-Null
$glbUrl = "https://$StorageAccount.blob.core.windows.net/twin-assets/head_with_muse.glb"
Ok "GLB uploaded -> $glbUrl"

Section "Entra Service Principal + ADT RBAC"
$existingSp = az ad sp list --display-name $SpName --query "[0].appId" -o tsv 2>$null
if (-not $existingSp) {
  $spJson = az ad sp create-for-rbac --name $SpName --skip-assignment | ConvertFrom-Json
  $clientId = $spJson.appId
  $clientSecret = $spJson.password
  $tenantId = $spJson.tenant
  Ok "Service Principal $SpName created"
} else {
  $clientId = $existingSp
  $tenantId = az account show --query tenantId -o tsv
  Write-Host "  ! Service Principal $SpName already exists; password not retrievable." -ForegroundColor Yellow
  Write-Host "    Rotate with: az ad sp credential reset --id $clientId" -ForegroundColor Yellow
  $clientSecret = "<rotate-and-paste-here>"
}

$existingAdtAssign = az role assignment list --assignee $clientId --scope $adtId `
  --role "Azure Digital Twins Data Owner" --query "[0].id" -o tsv 2>$null
if (-not $existingAdtAssign) {
  az role assignment create --assignee $clientId --role "Azure Digital Twins Data Owner" `
    --scope $adtId --only-show-errors | Out-Null
  Ok "Granted SP Data Owner on $AdtName"
}

Section "Configuration snippet for CmeSim.Api"
$config = [pscustomobject]@{
  AzureDigitalTwins = [pscustomobject]@{
    Endpoint     = $adtEndpoint
    TenantId     = $tenantId
    ClientId     = $clientId
    ClientSecret = $clientSecret
    GlbUrl       = $glbUrl
    SyncMode     = "Summary"
    SyncIntervalSeconds = 30
  }
}
$config | ConvertTo-Json -Depth 5

Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "  1. dotnet user-secrets set ""AzureDigitalTwins:ClientSecret"" ""<value>"" --project CmeSim.Api"
Write-Host "  2. Set the other AzureDigitalTwins:* values in CmeSim.Api/appsettings.Development.json"
Write-Host "  3. Run CmeSim.Api; the DigitalTwinBootstrapper will create User/Headband/4 Electrode twins on startup."
Write-Host "  4. Open https://explorer.digitaltwins.azure.net and attach to $adtEndpoint to verify."
