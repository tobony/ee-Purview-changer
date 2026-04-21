# Copilot Instructions for ee-Purview-changer

## Product intent

- This repository is a **Windows 11 Microsoft Purview single-file label change MVP**.
- Keep the product focused on **one-file inspection, preview, apply, and audit logging**.
- Prefer extending the existing MVP flow instead of introducing bulk-processing behavior.

## Solution structure

- `Ee.PurviewChanger.Desktop`
  - WPF shell and user interaction
  - Reads `appsettings.json`
  - Composes inspection/change/auth services
- `Ee.PurviewChanger.Core`
  - Domain models
  - Inspection and change services
  - Audit logging
  - Purview capability catalog
  - MIP SDK abstraction and native bridge seam
- `Ee.PurviewChanger.Core.Tests`
  - MSTest unit tests for planner, services, factory routing, and native client guardrails

## Architecture rules

- Keep UI-specific logic in `Ee.PurviewChanger.Desktop`.
- Keep inspection, preview, apply, audit-log, and mode-specific logic in `Ee.PurviewChanger.Core`.
- Do not let `MainWindow` directly implement labeling rules that already belong in Core services.
- Route live-mode local labeling through `IMipSdkFileLabelClient`.
- Use `MipSdkFileLabelClientFactory` to choose between development fallback and native client paths.
- Preserve the shared UI flow across Validation mode and Live mode.

## Execution modes

- `validationMode.enabled=true`
  - Use `LocalFileInspectionService`
  - Use `ValidationModeChangeService`
  - Do not perform real label changes
  - Record audit logs for simulated requests
- `validationMode.enabled=false`
  - Use `MipSdkFileInspectionService`
  - Use `MipSdkLabelChangeService`
  - Resolve the live-mode client through `MipSdkFileLabelClientFactory`

## Live mode extension guidance

- Preserve and extend `FileInspectionStatus` and `LabelChangeStatus` instead of replacing them with generic errors.
- Keep same-label blocking before apply and keep recheck validation after apply.
- If you add a real MIP SDK adapter, implement it behind `IMipSdkFileLabelClient` or `IMipSdkNativeBridge`.
- If you add cloud-file scenarios, create a separate service boundary instead of overloading local-file services.

## Configuration expectations

- App configuration is loaded from `Ee.PurviewChanger.Desktop/appsettings.json`.
- Default supported file extensions and candidate labels live in configuration.
- Placeholder values like `YOUR-...` are treated as incomplete configuration.

## Testing expectations

- Add or update tests in `Ee.PurviewChanger.Core.Tests` when changing planner logic, status mapping, service behavior, or MIP client routing.
- Prefer unit tests for status transitions and guardrail behavior.
- Keep documentation aligned with behavior when execution modes or supported capabilities change.

## Build and test

Build:

```bash
dotnet build /home/runner/work/ee-Purview-changer/ee-Purview-changer/Ee.PurviewChanger.slnx -p:EnableWindowsTargeting=true
```

Test:

```bash
dotnet test /home/runner/work/ee-Purview-changer/ee-Purview-changer/Ee.PurviewChanger.slnx -p:EnableWindowsTargeting=true
```

## Change style

- Make small, focused changes.
- Update related documentation when changing current behavior or extension points.
- Do not remove existing guardrails unless they are intentionally replaced with stricter behavior.
- Prefer explicit status handling over hidden fallback behavior.
