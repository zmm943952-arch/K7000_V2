# Test Plan Editor UI Design

**Goal:** Make the test plan maintenance page easier to scan and safer to edit after functional checks were grouped.

**Approved scope:** First phase only: group visibility, selected-item details, and validation reasons.

## Design

- Keep the current editable test item grid as the primary surface.
- Reduce the grid to high-signal columns: enabled, ID, step, kind, required, stop-on-failure, timeout, and a summary.
- Show grouped functional checks as one parent row with a child-count summary, for example `5 child items; shared power: CH1 12.2V`.
- Add a right-side detail panel bound to the selected grid row. It shows full ID, name, kind, timeout, script path, adapter, operation, limits, group children, and raw parameters JSON.
- Add a validation panel below the editor. It lists concrete reasons such as duplicate IDs, missing ID/name, invalid kind, invalid parameters JSON, empty functional group, missing child name/template, invalid timeout, script path missing, and low limit greater than high limit.
- Keep saving behavior compatible: the existing `ToDefinition()` path still writes the JSON test plan.

## UX Rules

- The page is an industrial maintenance tool, so density is acceptable but fields must have clear hierarchy.
- Avoid wide path columns in the grid; long paths belong in the detail panel.
- Error messages must name the exact item and field.
- No decorative visuals; use restrained blue/gray styling consistent with the current app.

## Verification

- Add ViewModel tests for group summaries and validation messages.
- Run the full solution tests before committing.
