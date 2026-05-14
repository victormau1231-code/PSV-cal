# V2 Preparation Notes

## Goal

Prepare the codebase for a strict clause-driven V2 without breaking V1 usability.

## What Is Already Prepared

- Standard profile abstraction:
  - `IStandardProfileProvider`
  - `JsonStandardProfileProvider`
  - Embedded profile JSON under `Data/standards`
- Validation abstraction for onsite benchmark cases:
  - `IValidationCaseStore`
  - `IValidationCaseRunner`
  - `JsonValidationCaseStore`
  - `ValidationCaseRunner`
- Startup template bootstrap:
  - `onsite-cases.template.json` is copied to `%USERPROFILE%\Documents\PSVCalc\Validation`
- Calculator coefficient path is now profile-aware:
  - default `Kd/Kb/Kc`, atmospheric pressure, steam Z bounds, universal gas constant can come from profile.

## Immediate Next V2 Tasks

1. Replace simplified steam property correlation with clause-aligned equations/tables.
2. Replace branch formulas with explicit API/ASME clause-mapped implementations.
3. Add coefficient provenance per formula step in expert view.
4. Add automated validation command/report for the onsite case set.
5. Freeze and version profile JSON when standards update.

## Acceptance Baseline

- Use 3-5 plant cases in `%USERPROFILE%\Documents\PSVCalc\Validation\onsite-cases.template.json`.
- Expected pass criteria:
  - area deviation within project threshold
  - recommended orifice match with onsite worksheet
  - no unclassified warning for accepted cases

