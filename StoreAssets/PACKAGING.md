# Packaging HueControl for the Microsoft Store (Visual Studio 2026)

Publisher: **Aegis AO Soft LLC** (Company developer account).

Everything visual is ready in `StoreAssets/`:
- `Images/` — all tile/logo/splash assets generated from the app icon.
- `Package.appxmanifest` — manifest with display name, logos and full-trust capability.
- `Images/StoreListing-300x300.png` — for the Partner Center listing (not the package).

## One-time: reserve the app

1. Partner Center → Apps and games → New product → reserve the name **HueControl**.
2. Note **Product Identity** (Package Name, Publisher `CN=...`) — used by "Associate App with the Store" below.

## Add the packaging project in Visual Studio 2026

1. Open `HueControl.slnx`.
2. Solution → Add → New Project → **Windows Application Packaging Project** → name it `HueControl.Package`.
   - If the template is missing: Visual Studio Installer → Modify → Individual components → add **MSIX Packaging Tools** (and the **Windows 11 SDK**).
3. In `HueControl.Package` → **Dependencies → Add → Project Reference** → check **HueControl** → OK.
   Right-click that reference → **Set as Entry Point** if prompted.
4. Replace the project's generated files:
   - Copy `StoreAssets/Images/*` into `HueControl.Package/Images/` (overwrite defaults).
   - Replace `HueControl.Package/Package.appxmanifest` with `StoreAssets/Package.appxmanifest`.

## Bind the Store identity (fills Publisher automatically)

5. Right-click `HueControl.Package` → **Publish → Associate App with the Store**.
6. Sign in with the **Aegis AO Soft LLC** company account → pick the reserved **HueControl** →
   Visual Studio writes the correct `Identity Name` / `Publisher` into the manifest.

## Build the upload package

7. Right-click `HueControl.Package` → **Publish → Create App Packages…**
8. Choose **Microsoft Store using…** → select the app → Release / x64 (and arm64 if you want) →
   Create. Output is a **`.msixupload`** file.

> No code-signing certificate needed: the Store re-signs the package with the Aegis AO Soft LLC
> publisher identity on submission.

## Submit

9. Partner Center → your HueControl product → **Packages** → upload the `.msixupload`.
10. Fill the listing (description, category = *Utilities & tools*, screenshots, the 300x300 image),
    complete the age-rating questionnaire (this app → suitable for all), then **Submit** for certification.

## Notes

- App is full-trust WPF; it runs outside the sandbox, so LAN access to the Hue bridge works without
  extra network capabilities.
- Bump `Version` in the manifest (e.g. 1.0.1.0) for every new submission.
