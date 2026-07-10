# Firmware Cache Delete Safety Design

## Goal

Prevent firmware preparation scripts from recursively deleting files outside directories that the application explicitly owns as disposable firmware caches.

## Scope

Apply the protection to the RFP and RedCase preparation scripts in both runtime trees:

- `Runtime/Flash/...`
- `src/RfpTestStation/StationRuntime/Flash/...`

TDDI currently copies a selected file without recursively clearing its destination, so it is outside this change.

## Safety model

The allowed roots are fixed by deployment layout rather than by configuration:

- RFP: the `Firmware` sibling of the script's parent `RFP_Auto` directory.
- RedCase: the exact `Firmware_Local` child of the script's `Debug` directory.

The script normalizes both root and target with `Path.GetFullPath` and rejects a reparse-point allowed root. RFP performs ordinal-ignore-case strict-descendant containment using a trailing directory separator, preventing sibling-prefix paths such as `FirmwareEvil` from matching `Firmware`. RedCase requires exact ordinal-ignore-case equality with its fixed `Firmware_Local` directory. A configured `LocalFirmwarePath` may be used only when all conditions hold:

1. Its canonical absolute path satisfies the module-specific rule above: strict descendant for RFP, exact fixed path for RedCase.
2. It is not the drive root, config directory, or MES source directory. The allowed-root-self prohibition applies to RFP; RedCase intentionally targets its exact fixed root.
3. Neither the target, any existing path segment between the allowed root and target, nor any entry anywhere in an existing target subtree is a reparse point. Validation completes before recursive deletion; the script never relies on recursive-delete link traversal behavior.
4. A non-empty existing target contains the script-owned marker `.rfp-firmware-cache`.

For a missing or empty target, the script creates the directory and marker before copying firmware. A pre-existing non-empty directory without the marker is rejected without deleting anything. The marker remains after cleanup.

## Failure behavior

Safety and ownership validation occurs before the existing "already current" fast path as well as before cleanup. Unsafe or unowned paths terminate preparation with a clear error. No delete or copy operation occurs after validation fails. Existing firmware and unrelated files remain untouched.

## Tests

PowerShell integration tests will verify:

- missing and empty targets receive the ownership marker;
- a valid, marked non-empty cache is cleared while preserving the marker;
- a non-empty unmarked directory is rejected and its sentinel remains;
- RFP paths outside/equal to its allowed root and sibling-prefix paths are rejected; RedCase paths other than its exact fixed directory are rejected;
- reparse points in the root-to-target chain and nested deletion subtree are rejected;
- each behavior is exercised independently for RFP and RedCase;
- the runtime and StationRuntime script copies stay identical.
