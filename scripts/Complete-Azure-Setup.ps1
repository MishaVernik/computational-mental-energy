<#
.SYNOPSIS
  Finishes the Azure side of the digital twin setup, assuming the ADT instance
  already exists and the user is logged in via `az login`.
  Skips Service Principal creation; relies on DefaultAzureCredential for local dev.

.EXAMPLE
  ./scripts/Complete-Azure-Setup.ps1
  ./scripts/Complete-Azure-Setup.ps1 -Reset   # wipe all twins + models and re-upload with updated DTDL
#>

param(
  [string]$Subscription   = "9acd98d2-d87b-46b9-ab35-daaa52513f2c",
  [string]$TenantId       = "eb3c2905-6af4-42a4-bea7-349aa51df740",
  [string]$ResourceGroup  = "AzureForStartups",
  [string]$Location       = "westcentralus",
  [string]$AdtName        = "cme",
  [string]$AdtEndpoint    = "https://cme.api.wcus.digitaltwins.azure.net",
  [string]$StorageAccount = "cmedtmv",
  [string]$Container      = "twin-assets",
  [switch]$Reset
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path "$PSScriptRoot\.."

function Section($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Ok($msg)      { Write-Host "  + $msg" -ForegroundColor Green }

Section "Active subscription"
az account set --subscription $Subscription --only-show-errors | Out-Null
az account show --query "{name:name, id:id, tenant:tenantId, user:user.name}" -o table

Section "ADT data-plane RBAC"
$userOid = az ad signed-in-user show --query id -o tsv
$adtId   = az dt show --dt-name $AdtName --resource-group $ResourceGroup --query id -o tsv
$existingAdtAssign = az role assignment list --assignee $userOid --scope $adtId `
  --role "Azure Digital Twins Data Owner" --query "[0].id" -o tsv 2>$null
if (-not $existingAdtAssign) {
  az role assignment create --assignee $userOid --role "Azure Digital Twins Data Owner" `
    --scope $adtId --only-show-errors | Out-Null
  Ok "Granted Azure Digital Twins Data Owner on $AdtName"
} else {
  Ok "User already has Data Owner on $AdtName"
}
Start-Sleep -Seconds 10

Section "DTDL model upload"

if ($Reset) {
  Write-Host "  -Reset specified: deleting all twins, relationships, and existing models." -ForegroundColor Yellow
  # JMESPath identifiers starting with $ (e.g. $dtId) clash with PowerShell variable
  # interpolation no matter how they're escaped; parse JSON in PowerShell instead.
  $twinsJson = & az dt twin query --dt-name $AdtName --query-command "SELECT * FROM digitaltwins" -o json 2>$null
  $allTwins  = @()
  if ($twinsJson) {
    try {
      $parsed = $twinsJson | ConvertFrom-Json
      $allTwins = @($parsed.result | ForEach-Object { $_.'$dtId' } | Where-Object { $_ })
    } catch { $allTwins = @() }
  }

  foreach ($t in $allTwins) {
    $relJson = & az dt twin relationship list --dt-name $AdtName --twin-id $t -o json 2>$null
    if ($relJson) {
      try {
        $relIds = @(($relJson | ConvertFrom-Json) | ForEach-Object { $_.'$relationshipId' } | Where-Object { $_ })
        foreach ($r in $relIds) {
          & az dt twin relationship delete --dt-name $AdtName --twin-id $t --relationship-id $r --yes --only-show-errors 2>&1 | Out-Null
        }
      } catch { }
    }
  }
  foreach ($t in $allTwins) {
    & az dt twin delete --dt-name $AdtName --twin-id $t --yes --only-show-errors 2>&1 | Out-Null
  }
  $existingModels = (& az dt model list --dt-name $AdtName --query "[].id" -o tsv 2>$null) -split "`n" | Where-Object { $_ }
  # Models reference each other; retry until everything is gone or progress stalls.
  for ($pass = 0; $pass -lt 5 -and $existingModels.Count -gt 0; $pass++) {
    foreach ($m in $existingModels) {
      & az dt model delete --dt-name $AdtName --dtmi $m --only-show-errors 2>&1 | Out-Null
    }
    Start-Sleep -Seconds 2
    $existingModels = (& az dt model list --dt-name $AdtName --query "[].id" -o tsv 2>$null) -split "`n" | Where-Object { $_ }
  }
  Ok "Cleared $($allTwins.Count) twins and $($existingModels.Count) leftover models"
}

$existingDtmis = (az dt model list --dt-name $AdtName --query "[].id" -o tsv) -split "`n"
# Activity is uploaded first because both User and Session reference dtmi:cme:Activity;1.
$modelFiles = @(
  "$repoRoot\docs\dtdl\Activity.json",
  "$repoRoot\docs\dtdl\Window.json",
  "$repoRoot\docs\dtdl\Electrode.json",
  "$repoRoot\docs\dtdl\Headband.json",
  "$repoRoot\docs\dtdl\Session.json",
  "$repoRoot\docs\dtdl\User.json"
)
$expectedDtmis = @(
  "dtmi:cme:Activity;1","dtmi:cme:Window;1","dtmi:cme:Electrode;1",
  "dtmi:cme:Headband;1","dtmi:cme:Session;1","dtmi:cme:User;1"
)
for ($i = 0; $i -lt $modelFiles.Length; $i++) {
  $dtmi = $expectedDtmis[$i]
  if ($existingDtmis -contains $dtmi) {
    Ok "$dtmi already uploaded (use -Reset to re-upload with new schema)"
    continue
  }
  az dt model create --dt-name $AdtName --models $modelFiles[$i] --only-show-errors | Out-Null
  if ($LASTEXITCODE -ne 0) { throw "Failed to upload model $($modelFiles[$i])" }
  Ok "Uploaded $dtmi"
}

Section "Storage account + GLB"
$prevEAP = $ErrorActionPreference
$ErrorActionPreference = "Continue"

$existingSt = (& az storage account show --name $StorageAccount --resource-group $ResourceGroup --query name -o tsv 2>&1 | Out-String).Trim()
$showExit = $LASTEXITCODE
if ($showExit -ne 0 -or $existingSt -match "ERROR" -or [string]::IsNullOrWhiteSpace($existingSt)) {
  & az storage account create `
    --name $StorageAccount --resource-group $ResourceGroup --location $Location `
    --sku Standard_LRS --kind StorageV2 --only-show-errors 2>&1 | Out-Null
  if ($LASTEXITCODE -ne 0) { $ErrorActionPreference = $prevEAP; throw "Failed to create storage account $StorageAccount (name may be globally taken)" }
  Ok "Storage account $StorageAccount created"
} else {
  Ok "Storage account $StorageAccount already exists"
}

# Newer storage accounts default to AllowSharedKey=false and AllowBlobPublicAccess=false.
# 3D Scenes Studio needs anonymous GLB read; account-key auth avoids the AAD propagation delay
# that otherwise blocks the very first container-create with --auth-mode login.
& az storage account update --name $StorageAccount --resource-group $ResourceGroup `
  --allow-blob-public-access true --allow-shared-key-access true --only-show-errors 2>&1 | Out-Null
Ok "Storage account: shared-key + public-blob access enabled"
Start-Sleep -Seconds 5

$key = & az storage account keys list --account-name $StorageAccount --resource-group $ResourceGroup --query "[0].value" -o tsv 2>&1 | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($key) -or $key -match "ERROR") {
  $ErrorActionPreference = $prevEAP
  throw "Could not fetch storage account key"
}

# Also assign Blob Data Contributor to the signed-in user for future SDK use, but do not depend on it.
$stId = & az storage account show --name $StorageAccount --resource-group $ResourceGroup --query id -o tsv 2>&1 | Select-Object -First 1
$existingBlobAssign = (& az role assignment list --assignee $userOid --scope $stId --role "Storage Blob Data Contributor" --query "[0].id" -o tsv 2>&1 | Out-String).Trim()
if ([string]::IsNullOrWhiteSpace($existingBlobAssign)) {
  & az role assignment create --assignee $userOid --role "Storage Blob Data Contributor" --scope $stId --only-show-errors 2>&1 | Out-Null
  Ok "Granted Blob Data Contributor on $StorageAccount"
}

& az storage container create --name $Container --account-name $StorageAccount `
  --account-key $key --public-access blob 2>&1 | Out-Null
Ok "Container $Container ready (public blob read)"

$glb = "$repoRoot\cme-live-dashboard\public\head_with_muse.glb"
& az storage blob upload --account-name $StorageAccount --account-key $key `
  --container-name $Container --name "head_with_muse.glb" --file $glb --overwrite --only-show-errors 2>&1 | Out-Null
$glbUrl = "https://$StorageAccount.blob.core.windows.net/$Container/head_with_muse.glb"
Ok "GLB uploaded -> $glbUrl"

$cfgSrc = "$repoRoot\docs\scenes_studio\3DScenesConfig.json"
$cfgTmp = "$env:TEMP\3DScenesConfig.cme.json"
(Get-Content $cfgSrc) -replace '<storage-account>', $StorageAccount | Set-Content $cfgTmp
& az storage blob upload --account-name $StorageAccount --account-key $key `
  --container-name $Container --name "3DScenesConfig.json" --file $cfgTmp --overwrite --only-show-errors 2>&1 | Out-Null
Ok "3D Scenes Studio config uploaded"
$ErrorActionPreference = $prevEAP

Section "Local user-secrets (CmeSim.Api)"
Push-Location "$repoRoot\CmeSim.Api"
try {
  dotnet user-secrets init | Out-Null
  dotnet user-secrets set "AzureDigitalTwins:Endpoint" $AdtEndpoint | Out-Null
  dotnet user-secrets set "AzureDigitalTwins:TenantId" $TenantId | Out-Null
  Ok "Endpoint + TenantId stored in user-secrets"
  Write-Host ""
  dotnet user-secrets list
} finally { Pop-Location }

Section "Summary"
Write-Host "  ADT endpoint    : $AdtEndpoint"
Write-Host "  GLB URL         : $glbUrl"
Write-Host "  Scenes config   : https://$StorageAccount.blob.core.windows.net/$Container/3DScenesConfig.json"
Write-Host ""
Write-Host "Next:" -ForegroundColor Cyan
Write-Host "  1. cd CmeSim.Api ; dotnet run"
Write-Host "  2. Watch logs for 'DigitalTwinSyncService active' + 'Base twins ensured'."
Write-Host "  3. Open https://explorer.digitaltwins.azure.net and attach to $AdtEndpoint to see the 6 base twins."
Write-Host "  4. Open https://explorer.digitaltwins.azure.net/3dscenes and add a scene pointing at the GLB URL above."
