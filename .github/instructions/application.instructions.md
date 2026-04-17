---
applyTo: "src/PackageUploader.Application/**"
---

# Application — CLI Entry Point

## Overview

Console application using System.CommandLine + Microsoft.Extensions.Hosting. Each CLI command maps to an Operation class.

## Adding a New Operation

1. Create a config class in `Config/` — use `[Required]` annotations and `[OptionsValidator]` partial validator
2. Create an operation class in `Operations/` inheriting from `Operation` base class
3. Register in `ProgramExtensions.cs` via extension method pattern
4. Add CLI options/arguments in `ParameterHelper.cs`
5. Create a JSON template in `templates/` and update `schemas/PackageUploaderOperationConfigSchema.json`
6. Document in `Operations.md`

## Conventions

- Operations follow command pattern: each has `DoOperation()` method
- Configuration priority: CLI args → Config file → Defaults
- Auth method selected via `-a` flag, registered in `AddIngestionAuthentication()`
- Logging: Console at Error (or Info/Trace in verbose mode), File always at Trace
- Timestamp format: `"yyyy-MM-dd HH:mm:ss.fff"`
