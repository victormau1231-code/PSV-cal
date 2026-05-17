# PSV Calculator Pro V 1.3.2

Chemical pressure safety valve sizing software built with `.NET 8 + WPF`.

## Current Scope

- Offline desktop calculator for `Gas + Steam + Liquid` single-case sizing
- Selectable calculation basis:
  - `API 520/521 + ASME`
  - `HG/T 20570.2`
- Core workflow:
  - Input normalization (units + gauge/absolute conversion)
  - Critical/subcritical branch decision
  - Area calculation with correction factors (`Kd`, `Kb`, `Kc`)
  - API orifice recommendation (smallest satisfying + neighbors)
- Gas strategy:
  - Built-in presets (air, nitrogen, oxygen, hydrogen, helium, argon, methane, ethane, ethylene, propane, propylene, butane, acetylene, carbon monoxide, carbon dioxide, ammonia, hydrogen sulfide, sulfur dioxide, chlorine)
  - `Custom` option with manual property entry and source audit
- Steam strategy:
  - Auto-estimated engineering correlation with warning
- Tube rupture:
  - API guillotine-break path
  - HG/T 20570.2 clause `7.0.8` liquid-service relief-load path
- Thermal expansion:
  - popup helper for blocked-in liquid thermal expansion load estimation
- Trim material recommendation:
  - API-context seat/disc material review guidance by service condition
  - clean, steam/high-temperature, dirty/abrasive/two-phase, sour/NACE, and chloride/seawater presets
- Standard-aware result presentation:
  - `API`: area + orifice letter + shorthand + inlet/outlet size
  - `HG/T`: area + direct throat diameter

## V2 Preparation Added

- Standard profile loader (`IStandardProfileProvider`) with embedded JSON profile:
  - `src/PSVCalc.Core/Data/standards/api520-521-asme-2026-04-01.json`
- Validation engine for onsite benchmark sets:
  - `IValidationCaseRunner`
  - `IValidationCaseStore`
  - embedded template `src/PSVCalc.Core/Data/validation/onsite-cases.template.json`
- App startup now auto-prepares template file in local storage validation folder.
- UI now includes a `Validation Report` button to run the onsite case set and export CSV report.

## Default Local Storage

- `%USERPROFILE%\Documents\PSVCalc\Projects`
- `%USERPROFILE%\Documents\PSVCalc\History`
- `%USERPROFILE%\Documents\PSVCalc\Exports`
- `%USERPROFILE%\Documents\PSVCalc\Validation`

## Project Structure

- `src/PSVCalc.Core`: calculation engine, interfaces, profile/template resources
- `src/PSVCalc.App`: WPF desktop UI + MVVM interaction layer
- `tests/PSVCalc.Tests`: unit tests and regression scaffolding

## Build And Test

```powershell
cd ~\PSVCalc
dotnet build PSVCalc.sln
dotnet test tests\PSVCalc.Tests\PSVCalc.Tests.csproj
```

## Run

```powershell
cd ~\PSVCalc.App
dotnet run
```

## Portable Publish

To build a portable Windows package that includes the `.NET 8` runtime and can run on another `win-x64` machine without installing .NET:

```powershell
cd F:\AI\PSVCalc
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-Portable.ps1
```

Outputs:

- `publish\portable-selfcontained\PSV-Calculator-Pro-V1.3.2-portable-win-x64-singlefile`
- `publish\portable-selfcontained\PSV-Calculator-Pro-V1.3.2-portable-win-x64-multifile`
- `publish\portable-selfcontained\archives\PSV-Calculator-Pro-V1.3.2-portable-win-x64-singlefile.zip`
- `publish\portable-selfcontained\archives\PSV-Calculator-Pro-V1.3.2-portable-win-x64-multifile.zip`
- each package includes `Release-Notes.txt` and `CHANGELOG.md`

Notes:

- This package is `self-contained`, so the target PC does not need a separate `.NET` installation.
- Current package target is `Windows x64`.
- This is still a portable app, not an installer.

## V2 Onsite Case Preparation

1. Open `%USERPROFILE%\Documents\PSVCalc\Validation\onsite-cases.template.json`.
2. Fill 3-5 real plant cases:
   - replace each `input` with site worksheet values
   - fill `expectedRequiredAreaMm2`
   - fill `expectedOrificeLetter`
3. Use these cases as acceptance baseline for formula migration in V2.
4. In app, click `Validation Report`; the exported report is written to `%USERPROFILE%\Documents\PSVCalc\Exports`.
