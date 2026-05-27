# VPS deployment runbook (Windows Server, no virtualisation)

Single-host deploy on the VPS at `161.97.146.52` (Windows Server 2022). Five auto-starting NSSM services receive MindMonitor OSC, run inference, and serve the dashboard.

## Topology

```
phone (MindMonitor) --UDP 55772--> cme-bridge --SignalR--> cme-api :5000
                                                              |
                                                              +--> cme-qbackend :8001
                                                              +--> cme-flowclassifier :8002
                                                              +--> Azure SQL :1433 (outbound)
                                                              +--> Azure Digital Twins :443 (outbound, optional)

your browser --:3001--> cme-dashboard (static)
your browser --:5000 SignalR--> cme-api
```

## Prerequisites already on the box

| Tool       | Path                                                                 |
|------------|----------------------------------------------------------------------|
| .NET 8 SDK | `C:\Program Files\dotnet\dotnet.exe`                                 |
| Python 3.10| `C:\Users\misha\AppData\Local\Programs\Python\Python310\python.exe`  |
| Node 24    | `C:\Program Files\nodejs\node.exe`                                   |
| Git        | `C:\Program Files\Git\cmd\git.exe`                                   |

NSSM is downloaded into `bin/` by the install script on first run.

## One-time deploy

1. Optional Azure Digital Twins sync. Skip this whole step if you don't need ADT writes -- the streaming demo works without it (the API falls back to `NoOpDigitalTwinSyncService`).

   On a machine where `az login` works, mint a Service Principal:

   ```powershell
   $adtId = az dt show --dt-name cme --resource-group AzureForStartups --query id -o tsv
   az ad sp create-for-rbac --name "cme-vps-sp" --role "Azure Digital Twins Data Owner" --scopes $adtId
   ```

   Copy `scripts/.env.vps.example` -> `scripts/.env.vps` and fill in `AzureDigitalTwins__TenantId`, `__ClientId`, `__ClientSecret`, `__Endpoint`. `.env.vps` is gitignored.

2. Right-click PowerShell -> **Run as Administrator**. Then:

   ```powershell
   cd D:\WORK\computational-mental-energy
   .\scripts\Install-VpsServices.ps1
   ```

   First run takes ~8-12 min (mostly `npm ci`, `pip install`, `dotnet publish`). Re-runs (after `git pull`) take ~30 s; pass `-SkipBuild` to skip publish/build steps and only re-register services.

3. On your phone, open MindMonitor -> Settings -> OSC Stream:
   - **Host**: `161.97.146.52`
   - **Port**: `55772`
   - All bands enabled, FFT enabled
   - Start streaming

4. In your browser open `http://161.97.146.52:3001` and click **Start Session**. Within ~5 s the gauges should move.

## What the script does, step by step

The full script lives at [scripts/Install-VpsServices.ps1](../scripts/Install-VpsServices.ps1). High level:

1. Asserts elevation.
2. Downloads `nssm.exe` (2.24, win64) to `bin/`, prints SHA256.
3. Creates three inbound Windows Firewall rules (`CME OSC (MindMonitor)` UDP 55772, `CME API` TCP 5000, `CME Dashboard` TCP 3001).
4. Creates `.venv/` and `pip install -r` for `qbackend/`, `flow-classifier/`, `muse-bridge/`.
5. `dotnet publish CmeSim.Api -c Release -o publish/CmeSim.Api`.
6. `npm ci` + `npm install --no-save serve@^14` + `npm run build` in `cme-live-dashboard/`.
7. Reads `scripts/.env.vps` (if present) into a hashtable.
8. Registers/updates 5 NSSM services with auto-start, LocalSystem, log rotation, and restart-on-failure:

   | Service              | Command                                                                                              |
   |----------------------|------------------------------------------------------------------------------------------------------|
   | `cme-api`            | `dotnet.exe publish\CmeSim.Api\CmeSim.Api.dll` -- env `ASPNETCORE_URLS=http://0.0.0.0:5000` + ADT vars |
   | `cme-qbackend`       | `.venv\python.exe -m uvicorn app.main:app --host 0.0.0.0 --port 8001` (cwd `qbackend`)               |
   | `cme-flowclassifier` | `.venv\python.exe -m uvicorn app.main:app --host 0.0.0.0 --port 8002` (cwd `flow-classifier`)        |
   | `cme-bridge`         | `.venv\python.exe bridge.py --osc --osc-port 55772 --hub-url http://127.0.0.1:5000/eeg-stream` -- depends on `cme-api` |
   | `cme-dashboard`      | `node.exe node_modules\serve\build\main.js -s dist -l 3001` (cwd `cme-live-dashboard`)               |

9. Starts each service and waits up to 45 s for its listening port to appear.
10. Prints a summary table.

## Verification

```powershell
Get-Service cme-*
Get-NetTCPConnection -State Listen | Where-Object { $_.LocalPort -in 5000,3001,8001,8002 }
Get-NetUDPEndpoint                 | Where-Object { $_.LocalPort -eq 55772 }
Get-NetFirewallRule -DisplayName 'CME *' | Format-Table DisplayName, Enabled, Direction, Action
```

Tail the bridge log live:

```powershell
Get-Content D:\WORK\computational-mental-energy\logs\cme-bridge.out.log -Wait
```

Expected first useful line: `OSC server listening on 0.0.0.0:55772`, then per-window log entries once MindMonitor starts pushing.

In the browser DevTools (Network -> WS), confirm the SignalR negotiate against `http://161.97.146.52:5000/eeg-stream` returns 200 and a WebSocket is established.

If `AzureDigitalTwins__Endpoint` is populated, after ~30 s open [ADT Explorer](https://explorer.digitaltwins.azure.net), attach to `https://cme.api.wcus.digitaltwins.azure.net`, and query:

```
SELECT * FROM digitaltwins T WHERE T.$dtId = 'user-default'
```

The 9 derived indices should be populated.

## HTTPS domain (`cmeflow.entertainmentpl.com`)

IIS already owns ports 80/443 on this host. The CME stack stays on `:3001` / `:5000`; IIS terminates TLS and reverse-proxies:

| Public URL | Backend |
|------------|---------|
| `https://cmeflow.entertainmentpl.com/` | `http://127.0.0.1:3001` (dashboard) |
| `https://cmeflow.entertainmentpl.com/api/*` | `http://127.0.0.1:5000` |
| `https://cmeflow.entertainmentpl.com/eeg-stream` | `http://127.0.0.1:5000` (SignalR + WebSocket) |

**One-time setup** (elevated PowerShell on the VPS):

1. DNS: `A` record `cmeflow.entertainmentpl.com` → `161.97.146.52`.
2. Ensure `cme-api` and `cme-dashboard` are running.
3. Run:

   ```powershell
   cd D:\WORK\computational-mental-energy
   .\scripts\Setup-CmeDomainHttps.ps1 -LetsEncryptEmail you@entertainmentpl.com
   ```

4. Rebuild and restart the dashboard (uses same-origin API over HTTPS):

   ```powershell
   cd cme-live-dashboard
   npm run build
   Restart-Service cme-dashboard
   .\scripts\Redeploy-VpsServices.ps1   # if Program.cs CORS changed
   ```

5. Open `https://cmeflow.entertainmentpl.com/`.

Direct IP access (`http://161.97.146.52:3001`) still works. Remove the IIS site with `.\scripts\Setup-CmeDomainHttps.ps1 -Remove`.

## Day-2 operations

| Action                    | Command                                                              |
|---------------------------|----------------------------------------------------------------------|
| Tail any service log      | `Get-Content logs\cme-api.out.log -Wait`                             |
| Restart everything        | `Restart-Service cme-* -Force`                                       |
| Stop everything           | `Stop-Service cme-bridge, cme-dashboard, cme-api, cme-qbackend, cme-flowclassifier` |
| Re-deploy after `git pull`| `.\scripts\Install-VpsServices.ps1` (re-runs idempotently)           |
| Skip the slow builds      | `.\scripts\Install-VpsServices.ps1 -SkipBuild`                       |
| Tear it all down          | `.\scripts\Uninstall-VpsServices.ps1` (add `-PurgeArtifacts` to delete `publish/`, `.venv/`, `dist/`, `logs/`, `bin/`) |

NSSM services are `Automatic` start, so they come back after reboot on their own.

## Security caveats (read once)

- **UDP 55772 is world-open** after step 2. Anyone who finds the IP can spoof EEG packets. Acceptable for a personal demo; once anyone else uses the system, narrow it down to your phone's current public IP:

  ```powershell
  Set-NetFirewallRule -DisplayName 'CME OSC (MindMonitor)' -RemoteAddress <your.public.ip>
  ```

- **API on :5000 is plain HTTP** when hit directly by IP. Browsers should use `https://cmeflow.entertainmentpl.com` (IIS TLS + reverse proxy). See [HTTPS domain](#https-domain-cmeflowentertainmentplcom) above.

- **Service Principal secret in `scripts/.env.vps`.** Plain-text on disk; protected only by NTFS ACLs (Administrators + SYSTEM). Rotate periodically.

- **Shared multi-tenant host.** This VPS already runs IIS, MySQL, SQL Server, AD/LDAP, and other tenants' projects under `C:\Projects\`. The CME stack uses ports `5000, 3001, 8001, 8002` (TCP) and `55772` (UDP) only -- no conflicts as of deploy time, but verify with `Get-NetTCPConnection -State Listen` before adding new ports.

## What we did NOT change

- `qbackend/.env` (committed IBM Quantum credentials) -- still on the box; rotate the IBM token and untrack the file in a separate PR.
- The 8 duplicated `API_BASE = window.location.hostname === 'localhost' ? ... : ...` blocks in `cme-live-dashboard/src/` -- they auto-resolve correctly to `http://161.97.146.52:5000` when the page is served from `http://161.97.146.52:3001`.
- IIS, MySQL, SQL Server, AD services on this host.
- `MuseAthena` direct-BLE connection path -- the bridge runs in OSC mode (which is what MindMonitor speaks). A VPS without a BLE radio cannot pair directly with the headset; the phone is the BLE host and forwards via OSC.
