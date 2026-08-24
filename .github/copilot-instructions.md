# WinGet Command Not Found Development Guide

## Project Overview

`Microsoft.WinGet.CommandNotFound` is a PowerShell 7 **feedback provider** module that suggests WinGet packages when a native command isn't found, surfacing them via PowerShell's predictive IntelliSense (command-line predictor).

- **`src/`** - C# implementation of the feedback provider/predictor
  - `WinGetCommandNotFoundFeedbackPredictor.cs` - Implements `IFeedbackProvider`/predictor logic, queries WinGet for suggestions when a command isn't found
  - `PooledPowerShellObjectPolicy.cs` - Object pooling for PowerShell instances used to query WinGet
  - `ValidateOS.psm1` - OS/version validation helpers
  - `Microsoft.WinGet.CommandNotFound.psd1` - Module manifest
- **`tools/helper.psm1`** - Build/dev helper functions
- **`build.ps1`** / **`Microsoft.WinGet.CommandNotFound.build.ps1`** - Build entry points

## Requirements

- PowerShell 7.4+ (built on the `IFeedbackProvider` interface)
- [PSReadLine](https://www.powershellgallery.com/packages/PSReadLine/2.2.6) 2.2.6+ for predictive suggestions
- Microsoft.WinGet.Client 1.11.460+
- Experimental features `PSFeedbackProvider` and `PSCommandNotFoundSuggestion` must be enabled (the latter is on by default in PowerShell 7.5+):
  ```powershell
  Enable-ExperimentalFeature PSFeedbackProvider
  Enable-ExperimentalFeature PSCommandNotFoundSuggestion
  ```

## Building, Testing, and Running

### Setup

```powershell
.\build.ps1 -Bootstrap
```

### Building

```powershell
.\build.ps1 -Configuration Debug   # or -Configuration Release
```

### Running/Debugging

```powershell
Import-Module .\src\bin\Debug\net8.0\Microsoft.WinGet.CommandNotFound.psd1
```

> If the module is already loaded in your session, start a fresh session with `pwsh -NoProfile` before importing a newly built copy.

## Architecture & Key Patterns

- Implements PowerShell's **Subsystem Plugin Model** — see [How to create a feedback provider](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-feedback-provider) and [How to create a command-line predictor](https://learn.microsoft.com/powershell/scripting/dev-cross-plat/create-cmdline-predictor).
- On a command-not-found event, the predictor shells out to WinGet (via a pooled PowerShell instance, see `PooledPowerShellObjectPolicy`) to search for matching packages and surfaces install suggestions inline.

## Contributing

- Prefer the issue templates under `.github/ISSUE_TEMPLATE/`, which set the issue type and initial triage labels automatically.
- Review the CLA requirements described in `README.md`.
- CI runs via Azure Pipelines (`.pipelines/`).
