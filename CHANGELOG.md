# Changelog

All notable changes to `PSV Calculator Pro` are documented in this file.

The format is inspired by `Keep a Changelog`, adapted for this desktop engineering tool.

## [1.3.2] - 2026-05-15

### Added

- API-context valve trim material recommendation for seat and disc selection review.
- Material service-condition selector covering clean, steam/high-temperature, dirty/abrasive/two-phase, sour/NACE, and chloride/seawater service.
- Trim recommendation output in the result sheet, expert rows, JSON project snapshot, and exported report data.

### Changed

- App title, assembly metadata, and startup titles promoted to `PSV Calculator Pro V 1.3.2`.

## [1.3.1] - 2026-04-09

### Added

- Custom atmospheric-pressure input for gauge/absolute conversion and report traceability.
- Balanced-bellows `Kb` override checkbox so the default `Kb = 1.0` path and manual override path are both explicit.
- Expanded preset gas library with additional common industrial gases and hydrocarbons.
- Recalculation reminder beneath the primary action button after any input change.

### Changed

- Primary calculate action restyled to stand out more clearly in the header toolbar.
- Preset-gas checkbox text updated to `使用预置组分气体 / Use Preset Component Gas`.
- Portable package, assembly metadata, and startup titles promoted to `PSV Calculator Pro V 1.3.1`.

### Fixed

- Pressure conversion chain now honors user-entered atmospheric pressure instead of always falling back to standard atmosphere.
- Balanced-bellows warning/audit logic now distinguishes between default `Kb` behavior and manual override behavior.

## [1.3.0] - 2026-04-08

### Added

- Stronger selected/unselected contrast across tabs, combo items, and selected rows.
- Thermal-expansion relief-load popup for blocked-in liquid service using `V = B * H / (rho * Cp)`.
- Focus-triggered input hints for key parameters to support faster field entry and review.

### Changed

- `HG/T 20570.2` result presentation now shows direct throat diameter instead of API orifice designation.
- App title, assembly metadata, and portable package naming promoted to `PSV Calculator Pro V 1.3`.
- Export report content now follows the active standard, including direct throat-diameter output for HG/T cases.

### Fixed

- Thermal-expansion scenario can now backfill relief load from popup inputs without forcing a manual load first.
- Startup error dialog titles updated to the new product version.

## [1.2.0] - 2026-04-02

### Added

- `API 520/521 + ASME` vs `HG/T 20570.2` selectable calculation basis in the main workflow.
- Embedded `HG/T 20570.2-1995` standard profile with applicability and fire-height metadata.
- `Custom` gas option for manual gas-property entry when preset data are not used.
- HG/T tube-rupture liquid-service load calculation per clause `7.0.8`, including optional high-side normal-flow cap.
- Standard-aware tube-rupture explanation block in the scenario page.

### Changed

- Portable package, app title, and assembly metadata promoted to `PSV Calculator Pro V 1.2`.
- Wetted-area popup now follows the selected standard's grade-height limit (`7.6 m` for API, `7.5 m` for HG/T).
- Exported reports now include the selected calculation standard and HG/T tube-rupture normal-flow input when used.

### Fixed

- Standard metadata persistence between project save/load and report export.
- Gas preset workflow now keeps a stable `Custom` selection instead of leaving the user with an ambiguous manual state.

## [1.1.0] - 2026-04-02

### Added

- Portable self-contained `win-x64` publish flow that includes the required `.NET 8` runtime.
- Release package automation script with versioned output folders and zip archives.
- `Release-Notes.txt` generation for each portable package.
- Local project persistence, history tracking, export directory structure, and validation case workflow.
- Excel-style report export and standalone validation report export.
- API orifice recommendation output including shorthand designation and inlet/outlet size display.
- Fire scenario helper dialog for wetted area estimation.
- Liquid sizing support and selected two-phase sizing support based on API 520 Annex C `Omega` branches.
- Scenario coverage for overpressure, fire, tube rupture, and thermal expansion.

### Changed

- Main desktop UI restyled into a more report-oriented engineering layout.
- Top-level actions moved into a toolbar-style header for a more industrial software workflow.
- Input panels reorganized into tabs and scenario-focused groups.
- Result presentation reorganized into summary, detail, warning, history, and audit sections.
- Portable package outputs now use versioned folder names for easier handoff and archive management.

### Fixed

- Startup resource initialization issues caused by invalid WPF resource ordering.
- Repeated startup error dialog loops by limiting startup error message display to a single prompt.
- Numeric pressure input handling so decimal values can be entered normally.
- Expert view and history panel clipping/layout issues.
- Combo box display issues introduced during recent UI styling changes.
