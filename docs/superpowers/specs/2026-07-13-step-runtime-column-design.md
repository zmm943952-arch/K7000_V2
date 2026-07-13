# Step 运行时间列设计

## 目标

在操作员运行结果表中显示每个 Step 的实际运行时间，使操作员能够直接识别耗时步骤，同时保持表格简洁、易扫读。

## 范围

- 在“期望值”和“STATUS”之间增加本地化运行时间列：中文为“运行时间”，英文为“Runtime”。
- 复用 `StepResult.StartTime` 和 `StepResult.EndTime`，不改变测试执行、调度或报告流程。
- 已完成且时间有效时，以秒为单位保留两位小数，例如 `1.23 s`。
- Pending、Running 或时间不完整时显示 `—`，不显示误导性的零耗时。
- STATUS 继续作为最右侧结果列。

## 数据流

`StepResult.StartTime/EndTime` → `StepResultViewModel.Duration` → WPF DataGrid“运行时间”列。

`Duration` 初始化为 `—`。Pending、Running、Next 等所有未完成状态均保持或恢复为 `—`；完成结果进入 `MarkCompleted` 时，仅在时间区间有效时替换为格式化耗时。完成、失败、错误或中止结果只要时间有效，均显示实际耗时。

## 显示规则

- 使用 invariant culture 生成数值，避免系统区域设置导致小数点表现变化。
- 格式固定为 `0.00 s`。
- 结束时间早于开始时间视为无效数据，显示 `—`。
- 新列宽度保持紧凑，Step 和 Reason 仍获得主要可用宽度。
- 运行时间标题遵循现有语言切换链路：`MainViewModel` 提供本地化属性并触发语言变更通知，`MainWindow.xaml.cs` 将其应用到具名列标题。

## 测试

- ViewModel 测试覆盖有效耗时、缺失时间、负耗时，以及 Pending/Running 状态的 `—` 占位符。
- 本地化测试覆盖中文“运行时间”和英文“Runtime”及语言切换通知。
- XAML 结构测试验证具名列、绑定、列顺序，以及 STATUS 仍为最后一列；窗口代码测试验证本地化标题赋值。
- 运行完整解决方案测试并执行 `git diff --check`。

## 非目标

- 不增加实时跳动的秒表。
- 不改变 CSV/JSON 报告格式。
- 不新增毫秒/分钟动态单位或用户配置项。
