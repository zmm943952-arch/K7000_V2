# Station Config UI Redesign Design

Date: 2026-06-30

## Goal

Redesign the WPF station configuration page so it is safe for operators by default and still efficient for engineers. The current page exposes all settings in one long scroll view with heavy blue borders, long full-width text fields, duplicated save actions, and little validation feedback. The redesign should make common changes fast, risky settings harder to change accidentally, and configuration status visible before saving.

## Target Users

- Primary: line operators who occasionally confirm or update common paths and instrument addresses.
- Secondary: engineers who need complete access to scripts, instrument ports, FCT station values, and safety IO settings during setup or troubleshooting.

The page should default to the operator-safe view while keeping engineering fields available through clear grouping.

## Recommended Direction

Use a split configuration workspace:

- Fixed top action bar for page title, reload, save all, and status.
- Left group navigation for configuration sections.
- Right content area for the selected section.
- Lightweight cards for related fields inside a section.
- Advanced and safety-related groups separated from common startup and instrument settings.

This keeps the production UI stable and scannable without hiding engineering capabilities behind a wizard.

## Information Architecture

Left navigation sections:

1. Common
2. Startup
3. Flashing
4. Instruments
5. FCT
6. Safety
7. Advanced

Default selected section: `Common`.

Section contents:

| Section | Fields |
| --- | --- |
| Common | Test plan path, config JSON path, RFP script, TCON script, TDDI script, TDDI serial port, IO DAQ COM, scanner COM, oscilloscope host |
| Startup | Test plan path, config JSON path |
| Flashing | RFP script path, TCON script path, TCON bin file path, TDDI script path |
| Instruments | TDDI serial port, IO DAQ COM, scanner COM, oscilloscope host |
| FCT | FCT station |
| Safety | safety enabled, poll interval, emergency stop channel/value, light curtain channel/value, fixture down/up output channels, fixture up delay |
| Advanced | raw config metadata/status and future rarely used settings |

Common duplicates fields from other sections intentionally. It is the daily edit surface; the detailed sections remain the source for full context.

## Layout

Top action bar:

- Height: about 72 px.
- Left: title `站点配置`.
- Middle/right: compact status text, such as `已加载 Project/Config.json` or `3 项未保存`.
- Right buttons: `重新加载`, `保存全部配置`.
- Only one primary save button should be visually dominant.

Main area:

- Left navigation width: 180-220 px.
- Right content max width: no hard max on desktop, but cards should use readable field widths instead of stretching every input to full screen.
- Page background: soft neutral, not pure white.
- Content cards: white background, subtle border, 8 px or smaller radius.

Field layout:

- Use two-column form rows where values are short, such as COM ports and numeric channels.
- Use single-column rows for long paths.
- Path rows should be: label, text box, `浏览...` button, validation hint.
- Avoid centered field text. Use left-aligned values.
- Avoid bold blue input values. Use dark neutral text; reserve blue for focus and primary actions.

## Visual System

Recommended palette:

- Page background: `#F5F7FA`
- Card background: `#FFFFFF`
- Primary blue: keep existing brand blue `#005AA0`
- Accent/focus blue: `#0078D4`
- Text primary: `#1F2933`
- Text secondary: `#5E6B78`
- Border: `#D8E0E8`
- Error: `#B42318`
- Success: `#137333`
- Warning: `#B54708`

Typography:

- Page title: 24-28 px, bold.
- Section title: 18-20 px, semibold.
- Field label: 13-14 px, semibold.
- Input text: 14 px, regular or medium.
- Status/helper text: 12-13 px.

Controls:

- Primary button: blue background, white text, 40 px height.
- Secondary button: white or pale background, blue/dark text, border.
- Inputs: 34-38 px height, 1 px border, focus border blue.
- Checkboxes: keep native WPF checkbox unless a shared style already exists.

## Interaction Behavior

Navigation:

- Selecting a left section changes the right content only.
- The top action bar stays visible.
- If there are unsaved changes, left nav item can show a small changed indicator.

Saving:

- `保存全部配置` saves startup settings and station config together if both have changes.
- If validation fails, do not save. Move focus to the first invalid field and show a section-level error summary.
- Status text should clearly distinguish `已保存`, `未保存`, `保存失败`.

Reload:

- If unsaved changes exist, reload should ask for confirmation before discarding changes.
- If no changes exist, reload immediately refreshes from disk and updates status.

Browse:

- Script paths should use file picker filtered to `.bat`, `.ps1`, and all files.
- Firmware path should use file picker filtered to `.bin` and all files.
- Config/test plan paths should use `.json`.

## Validation Rules

Validate as the user edits and again before saving:

| Field type | Rule |
| --- | --- |
| Required path | Non-empty |
| Existing file path | File should exist; warn if missing |
| COM port | Must match `COM` plus number, e.g. `COM4` |
| IP/host | Must be non-empty; IP format warning when it looks like an invalid IPv4 |
| Numeric channel | Integer, non-negative unless existing hardware requires otherwise |
| Poll interval/delay | Integer greater than or equal to 0 |
| Safety enabled/trigger values | Boolean checkbox |

Missing files should be blocking for production save unless engineering override is explicitly added later. For now, make missing files validation errors.

## Engineering Notes

Expected WPF implementation impact:

- Add a settings-section selection property to `MainViewModel`.
- Add per-field validation state or a simple validation summary model.
- Keep existing config load/save repositories.
- Replace the current long `SettingsPageVisibility` ScrollViewer content with a two-column layout inside the same `MainWindow.xaml`.
- Preserve existing bindings where possible; rename only when a field's meaning changes.

Do not change runtime execution behavior as part of this UI redesign. This is a configuration editing experience change.

## Acceptance Criteria

- The settings page opens on `Common`.
- All current editable station config fields remain reachable.
- There is only one visually dominant save action for station configuration.
- Common fields are visible without vertical scrolling on a 1600 x 920 window where practical.
- Safety settings are not shown in the default Common view.
- Long path values remain readable and editable without breaking layout.
- Invalid COM/IP/numeric/path values show inline validation and block save.
- Reload protects unsaved edits.
- Existing unit tests pass.
- Add or update ViewModel tests for section selection, dirty state, and validation where feasible.

## Decisions

- Missing script/bin/config/test-plan paths block save. This is the safer default for production software because an incomplete saved configuration can fail later during a station run. If bench-mode draft saving is needed later, add it explicitly as an engineering-only override with visible warning text.
- The Common section directly reuses the same ViewModel properties as detailed sections. It is a curated daily edit surface, not a separate copy of the data. Editing a value in Common and then opening Flashing/Instruments should show the same value.
- COM ports remain free-text inputs for the first redesign pass, with validation for `COM` plus number. Automatic COM discovery/dropdowns can be added later after confirming hardware enumeration behavior on the station PC.
