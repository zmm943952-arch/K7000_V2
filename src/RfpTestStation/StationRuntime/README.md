# StationRuntime

This directory contains the runtime assets used by `RfpTestStation`.

## Layout

- `Config/Config.json`: station configuration loaded by the app and flash scripts.
- `Flash/RFP_Auto`: RFP flash scripts, project files, local firmware cache, and logs.
- `Flash/RedCase_Auto`: TCON flash executable, scripts, local firmware cache, and logs.
- `Flash/TDDI_Auto`: TDDI flash executable, scripts, local firmware cache, and logs.
- `Lib/Hardware`: hardware adapter DLLs and related runtime config files.

## Path Rules

- Paths in `Config/Config.json` are relative to the `Config` directory unless they are absolute paths.
- Flash scripts resolve `Config/Config.json` from their own location, so the whole `StationRuntime` tree can move together.
- Legacy `Project` assets are kept untouched; this directory is the organized runtime copy used by the current app.
