# Station Config UI Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign the WPF station configuration page into a safer split workspace with Common-first navigation, lighter visual hierarchy, unified save behavior, and basic field validation.

**Architecture:** Keep the current single-window WPF/MVVM structure. Add section-selection and validation state to `MainViewModel`, then replace the settings ScrollViewer content in `MainWindow.xaml` with a left navigation and right-side section panels that reuse the existing configuration bindings. Do not change station runtime execution behavior.

**Tech Stack:** .NET Framework 4.8, WPF XAML, C#, xUnit, existing `MainViewModel`, existing `ConfigRepository` and `AppSettingsRepository`.

---

## Reference Documents

- Spec: `docs/superpowers/specs/2026-06-30-station-config-ui-redesign-design.md`
- Existing XAML: `src/RfpTestStation/RfpTestStation.App/MainWindow.xaml`
- Existing ViewModel: `src/RfpTestStation/RfpTestStation.App/ViewModels/MainViewModel.cs`
- Existing tests: `src/RfpTestStation/RfpTestStation.Tests/App/MainViewModelTests.cs`

## File Structure

- Modify `src/RfpTestStation/RfpTestStation.App/ViewModels/MainViewModel.cs`
  - Add selected settings section state.
  - Add localized section labels and section visibility properties.
  - Add validation summary/status properties.
  - Block station config save when required configuration values are invalid.
- Modify `src/RfpTestStation/RfpTestStation.App/MainWindow.xaml`
  - Add lighter settings-specific styles.
  - Replace the current long settings scroll layout with top action bar, left navigation, and right panels.
  - Reuse existing field bindings.
- Modify `src/RfpTestStation/RfpTestStation.Tests/App/MainViewModelTests.cs`
  - Add tests for default section, section switching, validation, and save blocking.
- Optionally update `docs/superpowers/specs/2026-06-30-station-config-ui-redesign-design.md`
  - Only if implementation uncovers a small necessary deviation.

## Tasks

### Task 1: Settings Section State

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.App/ViewModels/MainViewModel.cs`
- Test: `src/RfpTestStation/RfpTestStation.Tests/App/MainViewModelTests.cs`

- [ ] **Step 1: Write failing default-section test**

Add a test asserting:

```csharp
var viewModel = new MainViewModel(repoRoot);

Assert.Equal("Common", viewModel.SelectedSettingsSection);
Assert.True(viewModel.IsCommonSettingsSection);
Assert.Equal(Visibility.Visible, viewModel.CommonSettingsSectionVisibility);
Assert.Equal(Visibility.Collapsed, viewModel.SafetySettingsSectionVisibility);
```

- [ ] **Step 2: Run the failing test**

Run:

```powershell
dotnet test .\src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj --filter FullyQualifiedName~MainViewModelTests
```

Expected: fail because section properties do not exist.

- [ ] **Step 3: Implement section properties**

In `MainViewModel.cs`, add:

- private field `_selectedSettingsSection = "Common"`
- public `SelectedSettingsSection`
- booleans and `Visibility` properties for Common, Startup, Flashing, Instruments, FCT, Safety, Advanced
- helper `IsSettingsSection(string name)`
- command properties are not required for first pass; XAML can bind buttons to `SelectedSettingsSection` through simple click command only if needed. Prefer using existing `RelayCommand` if a command is necessary.

- [ ] **Step 4: Raise dependent property changes**

When `SelectedSettingsSection` changes, raise property changed for every section boolean and visibility property.

- [ ] **Step 5: Run tests**

Run the same test command. Expected: all `MainViewModelTests` pass.

### Task 2: Validation and Save Blocking

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.App/ViewModels/MainViewModel.cs`
- Test: `src/RfpTestStation/RfpTestStation.Tests/App/MainViewModelTests.cs`

- [ ] **Step 1: Write failing invalid-COM test**

Add a test asserting:

```csharp
var viewModel = new MainViewModel(repoRoot) { IoDaqCom = "DAQ4" };

viewModel.SaveStationConfigCommand.Execute(null);

Assert.Contains("COM", viewModel.StationConfigStatusText);
Assert.Contains(viewModel.Logs, x => x.Contains("Station config validation failed"));
```

Also assert that the JSON value remains unchanged.

- [ ] **Step 2: Write failing invalid-number test**

Set `SafetyPollIntervalMs = -1`, execute save, assert save is blocked and status mentions poll interval.

- [ ] **Step 3: Write failing invalid-path test**

Set a required script path to an empty string, execute save, assert save is blocked and status mentions script/path.

- [ ] **Step 4: Run tests and verify failure**

Run:

```powershell
dotnet test .\src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj --filter FullyQualifiedName~MainViewModelTests
```

Expected: fail because save validation is missing.

- [ ] **Step 5: Implement validation**

In `MainViewModel.cs`, add:

- `public string StationConfigValidationText { get; private set; }`
- `public bool HasStationConfigValidationError`
- private `bool ValidateStationConfigEditor(out string message)`
- COM validation using `^COM[0-9]+$`
- non-empty required path validation
- non-negative validation for interval/delay/channel values
- simple host validation: non-empty, and if it has four dot-separated numeric parts, each part must be 0-255

Call validation at the start of `SaveStationConfig()`. If invalid:

- Set `StationConfigStatusText`
- Set `StationConfigValidationText`
- Add log entry `Station config validation failed: ...`
- Return without writing the file.

- [ ] **Step 6: Run tests**

Run the same test command. Expected: all `MainViewModelTests` pass.

### Task 3: Unified Save Status and Labels

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.App/ViewModels/MainViewModel.cs`
- Test: `src/RfpTestStation/RfpTestStation.Tests/App/MainViewModelTests.cs`

- [ ] **Step 1: Write failing label/status test**

Assert localized text exists for:

- `CommonSettingsSectionText`
- `StartupSettingsSectionText`
- `FlashingSettingsSectionText`
- `InstrumentSettingsSectionText`
- `FctSettingsSectionText`
- `SafetySettingsSectionText`
- `AdvancedSettingsSectionText`
- `SaveAllStationConfigButtonText`

- [ ] **Step 2: Run and verify failure**

Run `MainViewModelTests` filter. Expected: fail because labels do not exist.

- [ ] **Step 3: Implement localized properties**

Add label properties using existing `T(chinese, english)` helper.

- [ ] **Step 4: Update language change notifications**

Add the new label/status properties to `RaiseLocalizedPropertiesChanged()`.

- [ ] **Step 5: Run tests**

Run `MainViewModelTests` filter. Expected: pass.

### Task 4: XAML Settings Layout Redesign

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.App/MainWindow.xaml`

- [ ] **Step 1: Add settings-specific styles**

Add resources:

- `SettingsPageBackground`
- `SettingsCardBackground`
- `SettingsBorder`
- `SettingsPrimaryText`
- `SettingsSecondaryText`
- `SettingsSectionButton`
- `SettingsSectionButtonActive` if practical with triggers
- `SettingsCard`
- `SettingsFieldLabel`
- `SettingsInput`

Keep existing global run-page styles unchanged.

- [ ] **Step 2: Replace settings ScrollViewer content**

Replace only the `SettingsPageVisibility` ScrollViewer contents.

New structure:

- outer Grid background `#F5F7FA`
- top row: title, status, reload, save all
- main row: left section navigation, right selected-section content

- [ ] **Step 3: Build Common panel**

Show:

- startup paths
- flashing scripts
- instrument ports/host

Reuse existing bindings:

- `TestPlanPath`
- `ConfigJsonPath`
- `RfpFlashScriptPath`
- `TconFlashScriptPath`
- `TddiFlashScriptPath`
- `TddiSerialPort`
- `IoDaqCom`
- `ScannerCom`
- `OscilloscopeHost`

- [ ] **Step 4: Build detailed panels**

Add panels with `Visibility` bound to section visibility properties:

- Startup
- Flashing
- Instruments
- FCT
- Safety
- Advanced

Move all existing fields into the appropriate panels. Do not remove any current editable field.

- [ ] **Step 5: Add validation text display**

Show `StationConfigValidationText` near the top status area or above the content cards. Use error color and collapse behavior if feasible; otherwise empty text is acceptable.

- [ ] **Step 6: Build check**

Run:

```powershell
dotnet build .\src\RfpTestStation\RfpTestStation.sln
```

Expected: build succeeds.

### Task 5: Full Verification

**Files:**
- All modified files.

- [ ] **Step 1: Run MainViewModel tests**

Run:

```powershell
dotnet test .\src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj --filter FullyQualifiedName~MainViewModelTests
```

Expected: pass.

- [ ] **Step 2: Run full solution tests**

Run:

```powershell
dotnet test .\src\RfpTestStation\RfpTestStation.sln
```

Expected: pass.

- [ ] **Step 3: Build x86 app**

Run:

```powershell
dotnet build .\src\RfpTestStation\RfpTestStation.App\RfpTestStation.App.csproj -p:Platform=x86
```

Expected: pass.

- [ ] **Step 4: Manual visual check**

Launch the app and open Settings:

```powershell
Start-Process .\src\RfpTestStation\RfpTestStation.App\bin\x86\Debug\net48\RfpTestStation.App.exe
```

Expected:

- Settings opens with Common selected.
- Safety fields are not visible in Common.
- Left navigation switches sections.
- Top save/reload controls remain visible.
- Long paths do not overlap other controls.
- Invalid values show validation and block save.

## Non-Goals

- Do not change station run execution.
- Do not add automatic COM port scanning yet.
- Do not add engineering override for missing paths yet.
- Do not refactor the full `MainWindow.xaml` outside the settings page.
