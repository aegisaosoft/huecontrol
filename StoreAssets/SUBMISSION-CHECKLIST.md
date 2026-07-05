# HueControl — Microsoft Store submission checklist

Publisher: **Aegis AO Soft LLC** · Version: **1.0.0**

## Ready (prepared)
- [x] App builds clean in **Release** (0 warnings / 0 errors), 24 tests green.
- [x] App icon + full **Store image set** — `StoreAssets/Images/` (tiles, wide, splash, target-sizes, 300×300 listing).
- [x] **Package.appxmanifest** — display name, logos, `runFullTrust` — `StoreAssets/Package.appxmanifest`.
- [x] **Store listing text** (English) — `StoreAssets/store-listing.md`.
- [x] **Privacy policy** live: https://aegisaosoft.github.io/huecontrol/
- [x] Trademark disclaimer (independent app, not affiliated with Signify/Philips) — in the listing.

## To do (needs your account + Visual Studio 2026)

### 1. Reserve the app (Partner Center)
- [ ] Sign in with the **Aegis AO Soft LLC** company account (free; company verification via D-U-N-S or docs).
- [ ] Apps and games → New product → reserve name **HueControl**.
- [ ] Note **Product Identity** (Package Name + Publisher `CN=...`).

### 2. Package in Visual Studio 2026
- [ ] Solution → Add → New Project → **Windows Application Packaging Project** (`HueControl.Package`).
      (If missing: VS Installer → add **MSIX Packaging Tools** component.)
- [ ] In it: Dependencies → Add **Project Reference** → HueControl (set as entry point).
- [ ] Copy `StoreAssets/Images/*` into the project's `Images/`; replace its `Package.appxmanifest`
      with `StoreAssets/Package.appxmanifest`.
- [ ] Right-click project → **Publish → Associate App with the Store** → pick HueControl
      (this writes the correct Identity/Publisher automatically).
- [ ] Right-click → **Publish → Create App Packages → Microsoft Store** → build the **`.msixupload`**
      (Release; x64, add arm64 if wanted). No signing cert needed — the Store re-signs.

### 3. Screenshots — DONE
- [x] Captured in `StoreAssets/Screenshots/` (1920×1032 PNG): rooms (dark),
      scenes (dark), rooms (light), Settings → Devices, Settings → Accessories
      (battery/last-seen), Homes sidebar. Upload these in the listing.

### 4. Submit (Partner Center)
- [ ] Upload the `.msixupload`.
- [ ] Paste listing from `store-listing.md`; category **Utilities & tools**;
      add screenshots + the `StoreListing-300x300.png`.
- [ ] Privacy policy URL: https://aegisaosoft.github.io/huecontrol/
- [ ] Age ratings questionnaire → all "no" → suitable for all ages.
- [ ] Submit for certification.

## Notes
- Bump `Version` in both `HueControl.csproj` and the manifest for each new submission.
- The app is full-trust WPF; LAN access to the Hue bridge needs no extra capabilities.
