# WPF Functional Test Station Rewrite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a WPF production test application that performs the same production flashing, measurement, judgment, cleanup, and reporting functions as the old TestStand/LabVIEW station without requiring TestStand or LabVIEW runtime dependencies.

**Architecture:** Create a new SDK-style .NET Framework 4.8, x86 solution under `src/RfpTestStation`. Keep the legacy `Project` folder as reference assets. Production execution uses a WPF-owned stable `TestPlan` file plus station configuration; `profile.html` and `.seq` are historical references only. Split the rewrite into test plan loading/validation, a functional test workflow, adapter implementations for hardware and external scripts, and a WPF MVVM operator UI.

**Tech Stack:** C#, WPF, .NET Framework 4.8, x86, Newtonsoft.Json, HtmlAgilityPack, xUnit, existing net48/x86 DLLs, existing `bat` and `ps1` flashing scripts.

---

## Reference Documents

- Spec: `docs/superpowers/specs/2026-06-30-wpf-teststand-rewrite-design.md`
- Sequence export: `Project/Teststand/RFP Auto Test Sequence/profile.html`
- Original sequence: `Project/Teststand/RFP Auto Test Sequence/RFP Auto Test Sequence.seq`
- Existing config: `Project/Config.json`

## Current Direction

TestStand is now treated as historical reference, not a runtime design to clone and not a production execution source. Existing importer, expression evaluator, migration runner, and migration workflow builder are kept only as legacy comparison tools. New production work must move to an explicit WPF/C# stable test plan workflow:

- `FunctionalWorkflow` orchestrates the production test in domain terms.
- `TestPlan` is the only production flow source and defines ordered `TestItem` entries.
- Hardware adapters are called directly from workflow services.
- External flashing tools and existing `bat`/`ps1` scripts stay script-driven.
- LabVIEW-related behavior remains a placeholder until direct C# interfaces are confirmed.
- Validation gates on functional equivalence: PASS/FAIL, first failure, critical measurements, flashing logs, safety state, cleanup, and reports.

## File Structure

Create this new structure. Do not move or rewrite the legacy `Project` folder.

```text
src/RfpTestStation/
- RfpTestStation.sln
- Directory.Build.props
- RfpTestStation.Core/
  - RfpTestStation.Core.csproj
  - Abstractions/
    - IClock.cs
    - IExternalProcessRunner.cs
    - IStationAdapterRegistry.cs
    - IStepExecutor.cs
  - Expressions/
    - ExpressionEvaluationException.cs
    - ExpressionResult.cs
    - TestStandExpressionEvaluator.cs
  - Importing/
    - ProfileHtmlImporter.cs
    - ProfileImportException.cs
    - ProfileStepRow.cs
    - StepBlockBuilder.cs
  - Model/
    - ExecutionContext.cs
    - LimitDefinition.cs
    - ModuleCallDefinition.cs
    - RunMode.cs
    - SequenceDefinition.cs
    - SequenceDocument.cs
    - StepDefinition.cs
    - StepResult.cs
    - StepStatus.cs
    - StepType.cs
  - Reporting/
    - JsonResultWriter.cs
    - CsvResultWriter.cs
    - RunReport.cs
  - Running/
    - SequenceRunner.cs
    - StepExecutionException.cs
    - StepExecutorRegistry.cs
    - Executors/
      - ActionStepExecutor.cs
      - FlowControlExecutors.cs
      - LimitStepExecutors.cs
      - SequenceCallStepExecutor.cs
      - UnsupportedStepExecutor.cs
      - WaitStepExecutor.cs
  - Workflow/
    - FunctionalTestWorkflow.cs
    - TestItem.cs
    - TestItemKind.cs
    - TestItemResult.cs
    - WorkflowRunContext.cs
    - WorkflowResult.cs
    - WorkflowStepResultMapper.cs
  - StationPaths.cs
- RfpTestStation.Adapters/
  - RfpTestStation.Adapters.csproj
  - Config/
    - ConfigRepository.cs
    - StationConfig.cs
  - Flashing/
    - FlashAdapter.cs
    - FlashScriptMap.cs
    - ProcessRunner.cs
  - Hardware/
    - DaqVoltageAdapter.cs
    - ModbusIoAdapter.cs
    - OscilloscopeAdapter.cs
    - SignalGeneratorAdapter.cs
    - UsbI2cAdapter.cs
  - Mock/
    - MockStationAdapterRegistry.cs
    - MockStepAdapters.cs
- RfpTestStation.App/
  - RfpTestStation.App.csproj
  - App.xaml
  - App.xaml.cs
  - MainWindow.xaml
  - MainWindow.xaml.cs
  - ViewModels/
    - MainViewModel.cs
    - RelayCommand.cs
    - StepResultViewModel.cs
    - StepTreeNodeViewModel.cs
  - Views/
    - ConfigurationView.xaml
    - ManualDebugView.xaml
    - RunView.xaml
- RfpTestStation.Tests/
  - RfpTestStation.Tests.csproj
  - TestPaths.cs
  - Importing/
    - ProfileHtmlImporterTests.cs
    - StepBlockBuilderTests.cs
  - Expressions/
    - TestStandExpressionEvaluatorTests.cs
  - Running/
    - SequenceRunnerTests.cs
    - FailureCleanupTests.cs
  - Adapters/
    - FlashAdapterTests.cs
  - Reporting/
    - ResultWriterTests.cs
```

## Implementation Tasks

### Task 1: Scaffold The New Solution

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.sln`
- Create: `src/RfpTestStation/Directory.Build.props`
- Create: `src/RfpTestStation/RfpTestStation.Core/RfpTestStation.Core.csproj`
- Create: `src/RfpTestStation/RfpTestStation.Adapters/RfpTestStation.Adapters.csproj`
- Create: `src/RfpTestStation/RfpTestStation.App/RfpTestStation.App.csproj`
- Create: `src/RfpTestStation/RfpTestStation.App/App.xaml`
- Create: `src/RfpTestStation/RfpTestStation.App/App.xaml.cs`
- Create: `src/RfpTestStation/RfpTestStation.App/MainWindow.xaml`
- Create: `src/RfpTestStation/RfpTestStation.App/MainWindow.xaml.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/RfpTestStation.Tests.csproj`

- [x] **Step 1: Create folders**

Run:

```powershell
New-Item -ItemType Directory -Force src\RfpTestStation\RfpTestStation.Core, src\RfpTestStation\RfpTestStation.Adapters, src\RfpTestStation\RfpTestStation.App, src\RfpTestStation\RfpTestStation.Tests
```

Expected: folders exist under `src/RfpTestStation`.

- [x] **Step 2: Create the solution**

Run:

```powershell
dotnet new sln -n RfpTestStation -o src\RfpTestStation
```

Expected: `src/RfpTestStation/RfpTestStation.sln` exists.

- [x] **Step 3: Create shared build props**

Create `src/RfpTestStation/Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <PlatformTarget>x86</PlatformTarget>
    <Prefer32Bit>true</Prefer32Bit>
  </PropertyGroup>
</Project>
```

- [x] **Step 4: Create Core project**

Create `src/RfpTestStation/RfpTestStation.Core/RfpTestStation.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="HtmlAgilityPack" Version="1.11.72" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
</Project>
```

- [x] **Step 5: Create Adapters project**

Create `src/RfpTestStation/RfpTestStation.Adapters/RfpTestStation.Adapters.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\RfpTestStation.Core\RfpTestStation.Core.csproj" />
    <Reference Include="Fct.ModbusRtu">
      <HintPath>..\..\..\Project\FCT_Auto\Fct.ModbusRtu\bin\x86\Release\net48\Fct.ModbusRtu.dll</HintPath>
    </Reference>
    <Reference Include="YK_DAQ20081">
      <HintPath>..\..\..\Project\FCT_Auto\Acquire\bin\x86\Release\net48\YK_DAQ20081.dll</HintPath>
    </Reference>
    <Reference Include="USB2IICDll">
      <HintPath>..\..\..\Project\FCT_Auto\I2C\USB2IICDll.dll</HintPath>
    </Reference>
    <Reference Include="Oscil">
      <HintPath>..\..\..\Project\FCT_Auto\Osil\Oscil.dll</HintPath>
    </Reference>
    <Reference Include="SG">
      <HintPath>..\..\..\Project\FCT_Auto\SG\SG.dll</HintPath>
    </Reference>
    <Reference Include="ReadConfigLib">
      <HintPath>..\..\..\Project\ReadConfig\ReadConfigLib\bin\Release\net48\ReadConfigLib.dll</HintPath>
    </Reference>
    <Reference Include="PreparedArtifactPaths">
      <HintPath>..\..\..\Project\RFP_Auto\Scripts\PreparedArtifactPaths.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

- [x] **Step 6: Create WPF project**

Create `src/RfpTestStation/RfpTestStation.App/RfpTestStation.App.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\RfpTestStation.Core\RfpTestStation.Core.csproj" />
    <ProjectReference Include="..\RfpTestStation.Adapters\RfpTestStation.Adapters.csproj" />
  </ItemGroup>
</Project>
```

- [x] **Step 7: Create minimal WPF app entry point**

Create a minimal WPF app entry point so the empty `WinExe` project can build before the full UI is implemented.

Create `src/RfpTestStation/RfpTestStation.App/App.xaml`:

```xml
<Application x:Class="RfpTestStation.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
  <Application.Resources />
</Application>
```

Create `src/RfpTestStation/RfpTestStation.App/App.xaml.cs`:

```csharp
using System.Windows;

namespace RfpTestStation.App
{
    public partial class App : Application
    {
    }
}
```

Create `src/RfpTestStation/RfpTestStation.App/MainWindow.xaml`:

```xml
<Window x:Class="RfpTestStation.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="RFP Test Station" Width="1200" Height="760">
  <Grid>
    <TextBlock Text="RFP Test Station" HorizontalAlignment="Center" VerticalAlignment="Center" />
  </Grid>
</Window>
```

Create `src/RfpTestStation/RfpTestStation.App/MainWindow.xaml.cs`:

```csharp
using System.Windows;

namespace RfpTestStation.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
```

- [x] **Step 8: Create test project**

Create `src/RfpTestStation/RfpTestStation.Tests/RfpTestStation.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <IsPackable>false</IsPackable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\RfpTestStation.Core\RfpTestStation.Core.csproj" />
    <ProjectReference Include="..\RfpTestStation.Adapters\RfpTestStation.Adapters.csproj" />
  </ItemGroup>
</Project>
```

- [x] **Step 9: Add projects to solution**

Run:

```powershell
dotnet sln src\RfpTestStation\RfpTestStation.sln add src\RfpTestStation\RfpTestStation.Core\RfpTestStation.Core.csproj src\RfpTestStation\RfpTestStation.Adapters\RfpTestStation.Adapters.csproj src\RfpTestStation\RfpTestStation.App\RfpTestStation.App.csproj src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj
```

Expected: all four projects are listed.

- [x] **Step 10: Build the empty solution**

Run:

```powershell
dotnet build src\RfpTestStation\RfpTestStation.sln -p:Platform=x86
```

Expected: build succeeds.

- [x] **Step 11: Commit when available**

If this workspace has been initialized as a git repository:

```powershell
git add src\RfpTestStation
git commit -m "chore: scaffold WPF test station solution"
```

If it is still not a git repository, skip the commit and continue.

### Task 2: Add Core Sequence Models

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Model/*.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/StationPaths.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/TestPaths.cs`

- [x] **Step 1: Write model tests**

Create `src/RfpTestStation/RfpTestStation.Tests/TestPaths.cs`:

```csharp
using System;
using System.IO;

namespace RfpTestStation.Tests
{
    internal static class TestPaths
    {
        public static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var profile = Path.Combine(dir.FullName, "Project", "Teststand", "RFP Auto Test Sequence", "profile.html");
                if (File.Exists(profile))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Cannot locate repository root from test output directory.");
        }

        public static string ProfileHtml()
        {
            return Path.Combine(RepoRoot(), "Project", "Teststand", "RFP Auto Test Sequence", "profile.html");
        }
    }
}
```

- [x] **Step 2: Add model files**

Implement immutable or simple mutable model classes for:

- `SequenceDocument`
- `SequenceDefinition`
- `StepDefinition`
- `ModuleCallDefinition`
- `LimitDefinition`
- `ExecutionContext`
- `StepResult`
- `RunMode`
- `StepType`
- `StepStatus`

Required minimum fields must match the spec flow model.

- [x] **Step 3: Add `StationPaths`**

`StationPaths` resolves legacy assets from the repo root:

```csharp
public sealed class StationPaths
{
    public StationPaths(string repoRoot) { RepoRoot = repoRoot; }
    public string RepoRoot { get; }
    public string ProfileHtmlPath => Path.Combine(RepoRoot, "Project", "Teststand", "RFP Auto Test Sequence", "profile.html");
    public string ConfigJsonPath => Path.Combine(RepoRoot, "Project", "Config.json");
}
```

- [x] **Step 4: Run model build**

Run:

```powershell
dotnet build src\RfpTestStation\RfpTestStation.Core\RfpTestStation.Core.csproj -p:Platform=x86
```

Expected: build succeeds.

### Task 3: Import `profile.html`

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Importing/ProfileHtmlImporter.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Importing/ProfileImportException.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Importing/ProfileStepRow.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/Importing/ProfileHtmlImporterTests.cs`

- [x] **Step 1: Write importer count tests**

Create tests that assert:

```csharp
var document = ProfileHtmlImporter.Load(TestPaths.ProfileHtml());
Assert.Equal(12, document.Sequences.Count);
Assert.Equal(820, document.Sequences.Sum(x => x.AllSteps.Count));
Assert.Contains(document.Sequences, x => x.Name == "MainSequence");
Assert.Equal(671, document.GetSequence("MainSequence").AllSteps.Count);
```

- [x] **Step 2: Run tests and verify failure**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter ProfileHtmlImporterTests
```

Expected: FAIL because importer does not exist.

- [x] **Step 3: Implement profile HTML loading**

Use `HtmlAgilityPack.HtmlDocument` to parse the export. Preserve raw text from the step, description, and settings columns.

Importer responsibilities:

- locate sequence headings
- locate setup/main/cleanup step tables
- normalize whitespace
- keep raw HTML row text for traceability
- assign stable incremental step ids

- [x] **Step 4: Add section count tests**

Assert `MainSequence` has:

```csharp
Assert.Equal(16, main.SetupSteps.Count);
Assert.Equal(627, main.MainSteps.Count);
Assert.Equal(28, main.CleanupSteps.Count);
```

- [x] **Step 5: Add burn step tests**

Assert the imported step names include:

- `MCU简易`
- `TCON`
- `TDDI`
- `MCU出货`

The source file contains Chinese names, so keep the test file encoded as UTF-8.

- [x] **Step 6: Run importer tests**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter ProfileHtmlImporterTests
```

Expected: PASS.

### Task 4: Build Flow Blocks

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Importing/StepBlockBuilder.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/Importing/StepBlockBuilderTests.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Core/Model/StepDefinition.cs`

- [x] **Step 1: Write block parser tests**

Create synthetic rows for:

- `If`
- `Else`
- `While`
- child action
- `End`

Assert child steps are attached under the correct parent and do not appear twice in execution order.

- [x] **Step 2: Run tests and verify failure**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter StepBlockBuilderTests
```

Expected: FAIL because block builder does not exist.

- [x] **Step 3: Implement indentation-based block building**

Use indentation and step type text from the exported row. Preserve original flat order in `AllSteps`; expose executable root steps through `SetupSteps`, `MainSteps`, and `CleanupSteps`.

- [x] **Step 4: Add real profile assertions**

Assert `Stop_Seq_Period` contains the safety monitor loop and that `MainSequence` contains nested `If` sections for debug mode.

- [x] **Step 5: Run block tests**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter StepBlockBuilderTests
```

Expected: PASS.

### Task 5: Implement Minimal TestStand Expression Evaluation

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Expressions/TestStandExpressionEvaluator.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Expressions/ExpressionResult.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Expressions/ExpressionEvaluationException.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/Expressions/TestStandExpressionEvaluatorTests.cs`

- [x] **Step 1: Write expression tests**

Cover only expression forms found in the exported sequence:

```csharp
Assert.True(EvalBool("!Locals.X2气缸到位", locals));
Assert.True(EvalBool("Locals.DebugMode", locals));
Assert.Equal(1.0, EvalNumber("TimeInterval(1)", locals));
Assert.True(EvalBool("RunState.SequenceFailed", context));
```

Include variable lookup for `Locals`, `FileGlobals`, `Parameters`, and `RunState`.

- [x] **Step 2: Run tests and verify failure**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter TestStandExpressionEvaluatorTests
```

Expected: FAIL because evaluator does not exist.

- [x] **Step 3: Implement evaluator incrementally**

Support:

- boolean literals
- numeric literals
- string literals
- `!`
- `&&`
- `||`
- `==`
- `!=`
- `<`
- `<=`
- `>`
- `>=`
- dotted variable lookup
- `TimeInterval(seconds)` as seconds

Return `ExpressionResult` with typed value and raw expression.

- [x] **Step 4: Add unsupported expression behavior**

Unsupported expressions must throw `ExpressionEvaluationException` with the raw expression and step name. Do not silently return default values.

- [x] **Step 5: Run expression tests**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter TestStandExpressionEvaluatorTests
```

Expected: PASS.

### Task 6: Implement Runner Core With Mock Execution

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Running/SequenceRunner.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Running/StepExecutorRegistry.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Running/Executors/*.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/Running/SequenceRunnerTests.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/Running/FailureCleanupTests.cs`

- [x] **Step 1: Write runner tests**

Create synthetic sequence tests for:

- setup/main/cleanup order
- skipped steps
- wait step using fake clock
- if true and if false
- while loop termination
- sequence call
- failure sets `SequenceFailed`
- cleanup runs after failure

- [x] **Step 2: Run tests and verify failure**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter SequenceRunnerTests
```

Expected: FAIL because runner does not exist.

- [x] **Step 3: Implement step executor registry**

Registry maps `StepType` to `IStepExecutor`. Unknown step types use `UnsupportedStepExecutor`, which returns `Error` unless the step is skipped.

- [x] **Step 4: Implement setup/main/cleanup execution**

`SequenceRunner.RunSequenceAsync` must:

- run setup
- run main
- run cleanup in `finally`
- record every step result
- support stop token
- surface first error while preserving cleanup results

- [x] **Step 5: Implement flow control executors**

Implement `Wait`, `If`, `While`, `For`, and `SequenceCall`.

- [x] **Step 6: Implement limit executors**

Implement numeric, string, pass/fail, and multiple numeric limit status mapping. Start with raw expected/actual values from parsed fields and mock adapter outputs.

- [x] **Step 7: Run runner tests**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter "SequenceRunnerTests|FailureCleanupTests"
```

Expected: PASS.

### Task 7: Add Adapter Contracts And Mocks

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Abstractions/IStationAdapterRegistry.cs`
- Create: `src/RfpTestStation/RfpTestStation.Adapters/Mock/MockStationAdapterRegistry.cs`
- Create: `src/RfpTestStation/RfpTestStation.Adapters/Mock/MockStepAdapters.cs`
- Modify: runner executors to call adapter registry.

- [x] **Step 1: Define adapter interfaces**

Add interfaces for:

- Modbus input/output
- power control
- USB I2C
- DAQ voltage
- oscilloscope
- signal generator
- config read
- external flashing
- SN read
- MES/PLC placeholders

- [x] **Step 2: Add mock adapters**

Mocks must return deterministic values for offline tests and allow tests to inject failures.

- [x] **Step 3: Add action dispatch tests**

Use imported real step names and VI/module paths to verify dispatch routing:

- `PowerControl.vi`
- `ReadI2C.vi`
- `ReadVolt.vi`
- `Test RFP_Flash_once.bat.vi`
- `Test RedCase_FlashUpdate_Run.bat.vi`
- `Test TDDI_Flash_once.bat.vi`

- [x] **Step 4: Run adapter dispatch tests**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter Adapter
```

Expected: PASS.

### Task 8: Implement External Flashing Adapter

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Abstractions/IExternalProcessRunner.cs`
- Create: `src/RfpTestStation/RfpTestStation.Adapters/Flashing/FlashAdapter.cs`
- Create: `src/RfpTestStation/RfpTestStation.Adapters/Flashing/FlashScriptMap.cs`
- Create: `src/RfpTestStation/RfpTestStation.Adapters/Flashing/ProcessRunner.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/Adapters/FlashAdapterTests.cs`

- [x] **Step 1: Write flash mapping tests**

Assert mappings:

```text
Support/Download/Test RFP_Flash_once.bat.vi -> Project/RFP_Auto/Scripts/Flash_once.bat
Support/Download/Test RedCase_FlashUpdate_Run.bat.vi -> Project/RedCase_Auto/Debug/FlashUpdate_Run.bat
Support/Download/Test TDDI_Flash_once.bat.vi -> Project/TDDI_Auto/Test/Test/bin/Debug/flash_run.bat
```

- [x] **Step 2: Run tests and verify failure**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter FlashAdapterTests
```

Expected: FAIL because adapter does not exist.

- [x] **Step 3: Implement `ProcessRunner`**

Use `System.Diagnostics.Process` with:

- configured working directory
- stdout capture
- stderr capture
- exit code capture
- timeout support
- cancellation support

- [x] **Step 4: Implement result parsing**

Map process results:

- exit code `0` and output/log text containing pass token -> `Passed`
- non-zero exit code -> `Failed`
- timeout/cancel -> `Error` or `Stopped`

Attach stdout, stderr, exit code, working directory, and log paths to the `StepResult.Message` or metadata.

- [x] **Step 5: Run flash tests**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter FlashAdapterTests
```

Expected: PASS using fake process runner. Do not execute real flashing scripts in unit tests.

### Task 9: Add Real Hardware Adapter Wrappers

**Files:**
- Create/modify: `src/RfpTestStation/RfpTestStation.Adapters/Hardware/*.cs`
- Create/modify: `src/RfpTestStation/RfpTestStation.Adapters/Config/*.cs`

- [x] **Step 1: Add compile-only wrappers**

Wrap existing DLL entry points without changing external libraries:

- `Fct.ModbusRtu.ModbusRtuClient`
- `Fct.Acquire.YkDaq20081Client`
- `USB2IICDll.TestStandApi`
- `Oscil.TestStandApi` or `Oscil.ScopeMeasurementClient`
- `SG.TestStandApi`
- `ReadConfigLib.JsonConfigReader`
- `PreparedArtifactPaths.PreparedArtifactPathsReader`

- [x] **Step 2: Add defensive error mapping**

Every hardware exception must become a failed `StepResult` with:

- adapter name
- method name
- step name
- original exception message

- [x] **Step 3: Add no-hardware compile test**

Run:

```powershell
dotnet build src\RfpTestStation\RfpTestStation.Adapters\RfpTestStation.Adapters.csproj -p:Platform=x86
```

Expected: build succeeds without connected hardware.

- [ ] **Step 4: Add manual smoke commands later**

These are not unit tests. Run only on the fixture PC with hardware available:

```powershell
dotnet run --project src\RfpTestStation\RfpTestStation.App\RfpTestStation.App.csproj -p:Platform=x86
```

Expected: manual debug screen can connect/read/write one device at a time.

### Task 10: Add Reporting And Logs

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Reporting/RunReport.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Reporting/JsonResultWriter.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Reporting/CsvResultWriter.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/Reporting/ResultWriterTests.cs`

- [x] **Step 1: Write report writer tests**

Assert generated file names:

```text
{SN}_{yyyyMMdd_HHmmss}_{Pass|Fail}.csv
{SN}_{yyyyMMdd_HHmmss}_{Pass|Fail}.json
```

Assert CSV includes:

- step name
- status
- value
- low limit
- high limit
- unit
- start/end time
- message

- [x] **Step 2: Run tests and verify failure**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter ResultWriterTests
```

Expected: FAIL because writers do not exist.

- [x] **Step 3: Implement JSON writer**

Use Newtonsoft.Json and preserve all step metadata.

- [x] **Step 4: Implement CSV writer**

Escape commas, quotes, and newlines. Use invariant culture for numeric values.

- [x] **Step 5: Run report tests**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter ResultWriterTests
```

Expected: PASS.

### Task 11: Build WPF Operator UI

**Files:**
- Create/modify: `src/RfpTestStation/RfpTestStation.App/*.xaml`
- Create/modify: `src/RfpTestStation/RfpTestStation.App/ViewModels/*.cs`
- Modify: `src/RfpTestStation/RfpTestStation.App/RfpTestStation.App.csproj`

- [x] **Step 1: Create app shell**

Create `App.xaml`, `App.xaml.cs`, `MainWindow.xaml`, and `MainWindow.xaml.cs`.

- [x] **Step 2: Create MVVM primitives**

Create `RelayCommand`, `MainViewModel`, `StepResultViewModel`, and `StepTreeNodeViewModel`.

- [x] **Step 3: Create main run view**

The first screen must be the usable production run screen, not a landing page.

Required UI:

- top operation bar with SN, current mode, current step, progress, start, stop, reset
- left sequence tree
- center result grid
- bottom log list
- tabs or side navigation for configuration and manual debug

- [x] **Step 4: Wire mock execution**

Start button:

- loads `profile.html`
- builds sequence model
- runs `MainSequence` with mock adapters first
- streams step results into the UI

Stop button:

- cancels execution
- leaves cleanup result visible

- [x] **Step 5: Build WPF app**

Run:

```powershell
dotnet build src\RfpTestStation\RfpTestStation.App\RfpTestStation.App.csproj -p:Platform=x86
```

Expected: build succeeds.

### Task 12: Wire Real Execution Mode

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.App/ViewModels/MainViewModel.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Adapters/Config/ConfigRepository.cs`
- Modify: adapter registry files.

- [x] **Step 1: Add execution mode setting**

Support at least:

- `Mock`
- `Hardware`

Default to `Mock` until hardware smoke checks pass.

- [x] **Step 2: Load `Config.json`**

Read `Project/Config.json` through `ConfigRepository`. Preserve unknown JSON fields when saving configuration.

- [x] **Step 3: Add hardware registry**

Create a registry that returns real adapters for hardware mode.

- [x] **Step 4: Run full offline tests**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86
```

Expected: PASS.

- [x] **Step 5: Run application**

Run:

```powershell
dotnet run --project src\RfpTestStation\RfpTestStation.App\RfpTestStation.App.csproj -p:Platform=x86
```

Expected: WPF app opens, imports the sequence, and runs mock execution without TestStand or LabVIEW.

Observed: UI Automation smoke opened the WPF app, entered a Mock SN, clicked Start, completed 656 steps, and generated PASS JSON/CSV reports.

### Task 13: Line Validation And Difference Checklist

**Files:**
- Create: `docs/validation/wpf-teststand-validation-checklist.md`

- [x] **Step 1: Create checklist**

Include:

- old TestStand run timestamp
- WPF run timestamp
- SN
- step count comparison
- skip comparison
- first failing step comparison
- final pass/fail comparison
- external flash log paths
- cleanup outputs observed
- intentional WPF differences and validation reason

- [ ] **Step 2: Run old-vs-new offline comparison**

Compare imported step coverage against `profile.html`. The first target is functional coverage and critical order dependencies, not a one-to-one clone of every TestStand internal step. Differences are allowed when they are documented as intentional WPF improvements.

- [ ] **Step 3: Run semi-hardware validation**

Validate one adapter at a time:

- Modbus IO
- flashing scripts
- USB I2C
- DAQ
- oscilloscope
- signal generator

- [ ] **Step 4: Run full-system validation**

Run the same product through old TestStand and WPF. Unintended functional differences must be fixed; intentional differences must be documented before production use.

### Task 14: Add Functional Workflow Layer

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Workflow/TestItemKind.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Workflow/TestItem.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Workflow/TestItemResult.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Workflow/WorkflowRunContext.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Workflow/WorkflowResult.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/Workflow/FunctionalTestWorkflow.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/Workflow/FunctionalTestWorkflowTests.cs`

- [x] **Step 1: Write workflow model tests**

Create tests that verify a workflow can represent these functional stages without referencing TestStand step internals:

```text
FixturePrepare
FlashMcuSimple
FlashTcon
FlashTddi
FlashMcuShipping
SafetyWait
FctMeasurement
ResultOutput
Cleanup
```

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter FunctionalTestWorkflowTests
```

Expected: FAIL because the workflow layer does not exist yet.

- [x] **Step 2: Implement workflow model classes**

Create simple domain classes:

```csharp
public enum TestItemKind
{
    FixturePrepare,
    Flash,
    SafetyCheck,
    Measurement,
    LimitCheck,
    ResultOutput,
    Cleanup
}
```

`TestItem` should include:

- `Id`
- `Name`
- `Kind`
- `SourceReference`
- `IsRequired`
- `Timeout`

`SourceReference` can point back to imported sequence names and step names for traceability without making TestStand the runtime model.

- [x] **Step 3: Implement `FunctionalTestWorkflow` execution skeleton**

The first implementation should run test items sequentially through injected delegates or services, record results, stop on required item failure, and always run cleanup items.

- [x] **Step 4: Add cleanup-on-failure tests**

Verify:

- required item failure marks workflow failed
- cleanup still runs
- optional item failure is recorded but does not stop the run unless configured

- [x] **Step 5: Run workflow tests**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter FunctionalTestWorkflowTests
```

Expected: PASS.

### Task 15: Map Migration Reference Steps To Functional Test Items

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Core/Workflow/MigrationWorkflowBuilder.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/Workflow/MigrationWorkflowBuilderTests.cs`

- [x] **Step 1: Write mapping tests using the real profile**

Use `profile.html` and assert that the builder creates functional items for:

- MCU simple RFP flash
- TCON / RedCase flash
- TDDI flash
- MCU shipping RFP flash
- safety wait / fixture-position checks
- FCT I2C checks
- oscilloscope checks
- AC input checks
- cleanup

- [x] **Step 2: Implement conservative name/path based mapping**

Map by step names, adapter/module paths, and known script paths. Keep unmapped imported steps as reference notes, not production workflow blockers.

- [x] **Step 3: Write unmapped-step report**

Produce a JSON or text summary listing:

- imported step count
- mapped functional item count
- skipped reference-only steps
- unmapped production-looking steps needing human review

- [x] **Step 4: Run mapping tests**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86 --filter MigrationWorkflowBuilderTests
```

Expected: PASS.

### Task 16: Let WPF Run The Functional Workflow In Mock Mode

**Files:**
- Modify: `src/RfpTestStation/RfpTestStation.App/ViewModels/MainViewModel.cs`
- Modify: `src/RfpTestStation/RfpTestStation.App/MainWindow.xaml`
- Modify: `src/RfpTestStation/RfpTestStation.Tests/App/MainViewModelTests.cs` if app test coverage is added

- [x] **Step 1: Add a workflow execution path behind the Start command**

Keep the existing migration runner available, but route normal Mock mode through `FunctionalTestWorkflow` once the builder can produce functional items.

- [x] **Step 2: Preserve report output**

Map `WorkflowResult` to the existing CSV/JSON report writers so report generation does not regress.

- [x] **Step 3: Update UI copy**

Use operator-facing terms such as "Production test workflow" and "Functional test item" instead of "sequence runner" where the UI is not specifically showing migration diagnostics.

- [x] **Step 4: Run full offline verification**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86
dotnet build src\RfpTestStation\RfpTestStation.sln -p:Platform=x86 --no-restore
```

Expected: all tests pass and the solution builds.

### Task 17: Add Stable TestPlan As The Production Execution Source

**Files:**
- Create: `src/RfpTestStation/Rfp7000V2.testplan.json`
- Create: `src/RfpTestStation/RfpTestStation.Core/TestPlans/TestPlanDefinition.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/TestPlans/TestPlanItemDefinition.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/TestPlans/TestPlanRepository.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/TestPlans/TestPlanValidationException.cs`
- Create: `src/RfpTestStation/RfpTestStation.Core/TestPlans/TestPlanWorkflowFactory.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/TestPlans/TestPlanRepositoryTests.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Core/StationPaths.cs`
- Modify: `src/RfpTestStation/RfpTestStation.App/ViewModels/MainViewModel.cs`

- [x] **Step 1: Write failing TestPlan repository tests**

Assert `src/RfpTestStation/Rfp7000V2.testplan.json` loads, has a name/version/product, contains ordered test items, rejects missing item IDs, and converts items to `TestItem` workflow entries.

- [x] **Step 2: Implement TestPlan model and repository**

Use Newtonsoft.Json. Keep parameters as structured JSON so each executor can read only the fields it owns.

- [x] **Step 3: Add the initial stable test plan**

Add a starter production plan with fixture prepare, flashing, safety, I2C, oscilloscope, AC input, result output, and cleanup items. Do not import or edit `profile.html`.

- [x] **Step 4: Route WPF Mock mode through TestPlan workflow**

Start button loads `TestPlan`, builds `FunctionalTestWorkflow`, executes mock item results, writes existing CSV/JSON reports, and logs the plan name/version.

- [x] **Step 5: Run full verification**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86
dotnet build src\RfpTestStation\RfpTestStation.sln -p:Platform=x86 --no-restore
```

Expected: all tests pass and the solution builds.

### Task 18: Add TestPlan Item Executor With Flash Support

**Files:**
- Create: `src/RfpTestStation/RfpTestStation.Adapters/TestPlans/TestPlanItemExecutor.cs`
- Create: `src/RfpTestStation/RfpTestStation.Adapters/TestPlans/ITestPlanFlashAdapter.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Adapters/Flashing/FlashAdapter.cs`
- Modify: `src/RfpTestStation/RfpTestStation.Adapters/Flashing/FlashScriptMap.cs`
- Modify: `src/RfpTestStation/RfpTestStation.App/ViewModels/MainViewModel.cs`
- Create: `src/RfpTestStation/RfpTestStation.Tests/TestPlans/TestPlanItemExecutorTests.cs`

- [x] **Step 1: Write failing executor tests**

Cover Mock mode, Hardware Flash item routing, and unsupported Hardware item behavior.

- [x] **Step 2: Implement TestPlan item executor**

Mock mode returns deterministic item results. Hardware mode supports `Flash` items through `ITestPlanFlashAdapter`; non-flash hardware items return explicit `Error` until their executors are wired.

- [x] **Step 3: Extend FlashAdapter for TestPlan parameters**

Support `parameters.flashKind`, `parameters.script`, and optional `parameters.workingDirectory`.

- [x] **Step 4: Route WPF through TestPlanItemExecutor**

WPF no longer keeps temporary item execution logic in `MainViewModel`.

- [x] **Step 5: Run full verification**

Run:

```powershell
dotnet test src\RfpTestStation\RfpTestStation.Tests\RfpTestStation.Tests.csproj -p:Platform=x86
dotnet build src\RfpTestStation\RfpTestStation.sln -p:Platform=x86 --no-restore
```

Expected: all tests pass and the solution builds.

## Plan Review Notes

This plan intentionally started with import and mock execution before hardware. That work remains useful as migration scaffolding and offline verification. The next phase should move production behavior into explicit functional workflow modules so the final WPF system is not a TestStand clone.

The plan does not add an Excel recipe system, an IO point table, or a C# rewrite of external flashing tools.
