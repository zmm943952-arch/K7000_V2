# Test Plan Flow Visibility Design

## Goal

Make the test-plan editor simple and immediately communicate the complete production test flow without removing existing editing capabilities.

## Information hierarchy

The page will present information in this order:

1. Plan identity and save actions.
2. Compact plan summary and execution-flow overview.
3. Stage-oriented test-item list.
4. Contextual detail editor for the selected item.

## Flow overview

Add a compact horizontal flow strip below the plan identity fields. It represents the actual current order of enabled items in the editor, not a canonical lifecycle legend and not the dependency scheduler. Consecutive items with the same stage are collapsed into one segment with a count; a stage that appears again later remains a separate segment. Thus `功能 → 测量 → 功能` stays visible rather than being merged into misleading coverage totals. A short caption explicitly says `按当前测试项顺序`.

Stage classification uses one presentation-only mapping shared by the grid and flow strip:

- `FixturePrepare` → `准备`
- `SafetyCheck` → `安全`
- `Flash` → `烧录`
- `Measurement` or `LimitCheck` → `测量`
- `FunctionalCheck` → `功能`
- `ResultOutput` → `结果`
- `Cleanup` → `清理`
- any unknown kind → `测试`

Existing ID-prefix fallback remains only where the legacy editor cannot parse a kind; it feeds this same mapping and falls back to `测试`. Disabled items are intentionally omitted and the strip is labeled accordingly. Dependencies and parallel groups remain available in `执行调度`; the strip does not claim to visualize them.

Beside the flow, show a concise plan summary: enabled item count, enabled-and-required item count, and the sum of positive timeout values for enabled top-level items. The exact label is `已启用项配置超时合计`; it is not a cycle-time prediction and excludes child/check delays already summarized within their owning top-level item.

## Test-item list

Keep the existing editable DataGrid and commands, but make stages visually scannable:

- Use localized headers: `序号`, `阶段`, `启用`, `测试项`, `流程摘要`.
- Apply subtle alternating row backgrounds and stronger stage text.
- Retain the phase column because true grouped rows would complicate editing, selection, reordering, and virtualization.
- Convert long parameter text into a compact flow summary already produced by the editor view model; full values remain in the detail panel.

## Command hierarchy

- Primary actions: `保存测试计划`, `新增模板`.
- Secondary plan-item actions: `复制`, `上移`, `下移`.
- Destructive action: `删除`, visually distinct but not larger.
- Child and check toolbars use shorter labels and compact sizing while keeping text labels for clarity.
- Disabled actions remain visibly disabled.

No commands are removed or moved into a context menu in this iteration, avoiding discoverability and automation regressions.

## Detail panel

- Rename the panel to `测试项配置`.
- Keep basic fields visible.
- Show the child/check editor only for functional-group items, as today.
- Keep `执行调度` and `高级参数` collapsed by default.
- Use clearer section spacing so nested child and check tables read as one hierarchy rather than unrelated tables.

## View-model data

Add read-only presentation properties to `MainViewModel`:

- plan summary text;
- an ordered immutable collection of stage-segment view models containing localized stage name, item count, and accessible description.

The values refresh when a plan loads/reloads; the collection is cleared or replaced; an item is added, inserted, duplicated, deleted, or reordered; or any item's enablement, kind, requirement, or timeout changes. The view model subscribes to every editor row, not only the selected row, and reliably detaches handlers during reload/removal. Because timeout is integer-bound, invalid transient text leaves the last valid value in the summary until binding becomes valid. These presentation values do not affect saved JSON or execution scheduling.

## Accessibility and scaling

- Preserve readable font sizes.
- Toolbar buttons use auto height, minimum height, and explicit centered content alignment.
- Do not encode stage meaning by color alone; every stage is labeled.
- Render stage segments in a wrapping `ItemsControl`/`WrapPanel`; no horizontal scrolling is required. At narrow widths or high DPI, segments wrap to another line and the Auto-sized row grows. The summary wraps independently above the strip. The existing grid/detail split keeps its current minimum width and scroll behavior.
- Stage text and muted surfaces meet at least WCAG AA contrast for normal text.
- Each segment exposes `AutomationProperties.Name` with localized stage name, count, and order. The informational strip is not focusable and does not enter the tab sequence; editing controls retain their current logical tab order.
- Stage captions and summary labels follow the existing Chinese/English language state rather than hardcoded XAML strings.

## Verification

- Add view-model tests for consecutive collapse, repeated/unknown stages, enabled/required/timeout edits on selected and non-selected rows, add/duplicate/delete/reorder/reload, subscription detachment, and localization changes. A WPF binding/UI test, rather than a pure view-model test, verifies invalid timeout text leaves the last valid integer summary unchanged.
- Add XAML source tests for flow order, localized headers, command presence, collapsed advanced sections, and scalable toolbar styles.
- Build and run the complete test suite.
- Launch the WPF application and visually verify 1280- and 1600-pixel window widths at the available display scaling, including wrapped flow segments, button labels, keyboard tab order, and contrast. If multiple Windows DPI settings are unavailable, record that limitation rather than claiming multi-DPI rendering coverage.

`高级参数` keeps its existing expansion-state behavior; this redesign does not reset or persist expansion differently.
