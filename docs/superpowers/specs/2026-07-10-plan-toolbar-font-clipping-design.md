# Plan Toolbar Font Clipping Design

## Goal

Ensure test-plan toolbar button labels render completely under Windows DPI and font scaling.

## Root cause

`PlanToolbarButton` fixes `Height` at 40 pixels and inherits another fixed height from `OperationButton`; it also does not explicitly set content alignment. Chinese semibold glyph metrics can exceed the available content box under display scaling, causing vertical clipping.

## Change

- Override the inherited fixed height with `Height="Auto"` and add `MinHeight="40"`, allowing the Auto-sized toolbar row and button to grow under DPI scaling.
- Explicitly set `HorizontalContentAlignment="Center"` and `VerticalContentAlignment="Center"`.
- Keep the current font size, padding, widths, colors, commands, and toolbar order.
- Apply through the shared `PlanToolbarButton` style so normal and danger buttons behave consistently.

## Verification

- Add a XAML regression test asserting the effective shared toolbar style explicitly overrides inherited height with `Auto`, uses minimum height, and sets content alignment.
- Build and run the full test suite.
- Visually verify the affected toolbar at normal and increased Windows display scaling when available.
