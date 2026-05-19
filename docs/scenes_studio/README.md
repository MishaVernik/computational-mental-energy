# Azure DT 3D Scenes Studio — Scene Config

This folder holds the pre-authored 3D Scenes Studio configuration for the CME
cognitive digital twin. Importing `3DScenesConfig.json` wires the
`head_with_muse.glb` mesh nodes to the live ADT twin properties created by
`DigitalTwinSyncService`.

## What you get when you load it

| Mesh node | Bound twin | Visual rule |
|---|---|---|
| `AF7`, `AF8`, `TP9`, `TP10` | `electrode-<pos>` | Colour ramp on `beta + theta` (blue → yellow → red); badge with live `beta/theta/alpha`. Pop-up with `quality`. |
| `Head` | `user-default` | Colour ramp on `currentPFlow` (grey < 0.5 → yellow 0.5–0.7 → green ≥ 0.7); badge with live `pFlow` and `CME rate`. |

These are the same numbers the local Three.js `HeadTwin3D` shows — by design,
so the two views stay in sync visually.

## Loading procedure

Prereqs (see [../azure_setup.md](../azure_setup.md)):

- ADT instance exists with the 5 DTDL models uploaded.
- Bootstrapper has run once, so the User / Headband / 4 Electrodes twins exist.
- The GLB is uploaded to `cmedtblob<initials>/twin-assets/head_with_muse.glb`.

Then:

1. Open <https://explorer.digitaltwins.azure.net/3dscenes>.
2. Choose **Add scene**. When prompted for the **3D file URL**, paste:

   ```
   https://<storage-account>.blob.core.windows.net/twin-assets/head_with_muse.glb
   ```

3. When prompted for **Configuration file storage**, point it at the same
   `twin-assets` container. 3D Scenes Studio will create
   `3DScenesConfig.json` there if absent.

4. To skip manual authoring, replace the auto-created blob with the file in
   this folder. Find-and-replace `<storage-account>` first:

   ```powershell
   $st = "cmedtblob<initials>"
   (Get-Content docs/scenes_studio/3DScenesConfig.json) `
     -replace "<storage-account>", $st `
     | Set-Content config.json
   az storage blob upload --account-name $st --container-name twin-assets `
     --name 3DScenesConfig.json --file config.json --auth-mode login --overwrite
   ```

5. Reload 3D Scenes Studio. You should see the scene **CME Cognitive Twin** with
   electrodes glowing on the head.

## How the bindings map to ADT properties

```
StatusColoring  expression                            from twin
─────────────── ─────────────────────────────────────── ────────────────────────────────────────
electrodes      PrimaryTwin.beta + PrimaryTwin.theta  electrode-AF7, electrode-AF8, electrode-TP9, electrode-TP10
head            PrimaryTwin.currentPFlow              user-default
```

Both expressions are evaluated server-side by Scenes Studio every time the
underlying twin is patched, which happens at the throttled cadence chosen in
`AzureDigitalTwinsOptions.SyncIntervalSeconds` (30 s by default). Lowering the
interval costs more (see [../digital_twin_platform.md](../digital_twin_platform.md)
§Cost envelope).

## Screenshots for the lab report

Capture these once the scene is live:

- `scenes-overview.png` — front view of the head with the 4 electrodes lit.
- `scenes-badge-popup.png` — hovering an electrode, showing the `beta/theta/alpha` badge.
- `scenes-quality-alert.png` — eye-roll artifact pushes one quality below 0.5; pop-up triggers.
- `scenes-pflow-green.png` — sustained `pFlow > 0.7` → head turns green.

Place them under `paper/results/scenes_studio/` and reference them in
[LAB_REPORT.md](../LAB_REPORT.md) Етап 6 / Етап 8.

## Fallback if Scenes Studio is unavailable

The local Three.js `HeadTwin3D` panel in `cme-live-dashboard` produces an
equivalent visualization driven directly by SignalR. The lab demo prioritises
this view; Scenes Studio screenshots are evidence of the cloud-platform layer
and are not on the critical path.
