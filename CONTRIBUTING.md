# Contributing to HueControl

Thanks for your interest in improving HueControl! This is a free, open-source
(GPL-3.0) Windows app for Philips Hue, and contributions are welcome.

## Ways to help

- **Report bugs** — open an issue with steps to reproduce, your Windows version,
  bridge model, and what you expected vs. what happened.
- **Suggest features** — open a feature request describing the problem you want solved.
- **Test on hardware** — HueControl talks to real bridges. Reports of what works (or
  doesn't) on different bridge models and device types are very valuable.
- **Improve docs** — README, code comments, and this guide.
- **Send code** — see below.

## Development setup

Requirements: Windows 10/11 and the **.NET 9 SDK**.

```powershell
git clone https://github.com/aegisaosoft/huecontrol.git
cd huecontrol
dotnet build
dotnet run --project HueControl
dotnet test
```

The solution has two projects:

- `HueControl` — the WPF app (MVVM).
- `HueControl.Tests` — xUnit tests. Most use a stubbed HTTP handler; a few optional
  live tests talk to a real bridge (set `HUE_BRIDGE_IP`).

## Pull requests

1. Fork the repo and create a branch (`git checkout -b my-fix`).
2. Keep changes focused — one topic per PR.
3. Match the existing code style (nullable enabled, MVVM, async APIs on `HueApiClient`).
4. Add or update tests where it makes sense; run `dotnet test` before pushing.
5. Write **code comments in English**.
6. Open the PR against `main` and fill in the template.

## Code style

- C# with nullable reference types enabled.
- UI logic stays in views; state and commands live in view models.
- All Hue REST calls go through `Services/HueApiClient`.
- Don't hard-code bridge IPs, tokens, or device IDs.

## Licensing

By contributing, you agree that your contributions are licensed under the
**GPL-3.0**, the same license as the project.

## Not affiliated with Signify

HueControl is an independent project and is not affiliated with, endorsed by, or
sponsored by Signify N.V. or Philips. Please don't add Philips/Hue branding, logos,
or copyrighted assets to the project.
