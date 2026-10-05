---
title: 烧录独立PASS脉冲修复
status: current
created: 2026-10-05
updated: 2026-10-05
source: TestPlanItemExecutor.ExecuteResultOutputAsync; StationSafeCleanup; MainViewModel.SaveFixtureFlowSettingsToTestPlan; dotnet test
---

# 烧录独立PASS脉冲修复

## 决策与已验证行为

客户旧 BurnOnly 包用 Y6 控制产品电源，物理结果灯关闭。PASS 脉冲现在由独立参数 enablePassPulse 控制，未提供参数时沿用 enablePhysicalResultOutputs 的默认策略，保留旧计划行为。不能仅开启物理结果灯来实现脉冲。

保存夹具流程配置会写入 enablePassPulse=true、界面脉冲通道及延时；旧软件结果模式缺少通道时，编辑器默认 Y8，避免默认 Y6 与产品电源冲突。执行器在 PASS 分支先按原计划断电，再输出 True，延时后输出 False。NG 不输出脉冲，复位处理不变。通道必须在 1-48，延时非负，脉冲通道不能与 productPowerOutputChannel 相同。

脉冲的 finally 使用独立 2 秒清理令牌关闭输出，取消或写 True 抛错时也尝试关闭；若真实通讯失效，软件无法保证物理输出已关闭。安全清理独立读取 enablePassPulse 并优先关闭脉冲输出，不受物理结果灯开关限制。

Runtime/TestPlans/Rfp7000BurnOnly.testplan.json 现设置 enablePassPulse=true、通道8、延时1000，保留当前计划的程控电源参数。该计划不是客户旧包的 Y6 供电计划，不应整份覆盖客户文件。

## 客户旧版本更新边界

客户旧 EXE 尚未替换，也未生成客户发布包。修复已编译到当前工作区 Debug 构建。客户使用时需更新匹配的应用与依赖组件，保留其原有 Runtime/Config、烧录资产及 Y6 供电计划；只在旧计划 result.output 中增加 enablePassPulse=true、passPulseOutputChannel=8、passPulseMs=1000，保持 enablePhysicalResultOutputs=false 和 productPowerOutputChannel=6。更新应用后通过“保存全部配置”也会保存独立脉冲开关。旧 EXE 不认识新增开关，单改配置无法完成更新。

现场仍需验证实际脉宽、Y6断电、Y8脉冲、NG无脉冲及停止/安全触发收尾。当前任务未执行现场动作或发布版本。

## 验证与备份

专项19/19通过：Y8保持至少1000 ms、独立脉冲不驱动灯、NG、取消、写出异常、供电通道冲突、安全清理、UI保存和计划同步/烧录计划验证。App/Core/Adapters/Tests构建成功。先前扩大检查的6项无关失败仍未处理，本次未宣称全量通过。

现有工作区含大量原有改动，且当前脉冲实现所依赖的基线尚未全部入库。仅本次差异保存于 docs/burn-only-independent-pass-pulse.patch，并检查可反向应用；恢复需先有本次开始时工作区基线，再执行 git apply --ignore-space-change docs/burn-only-independent-pass-pulse.patch，不能直接应用到裸 HEAD。此补丁不包含原有未提交改动，不代表完整工作区备份。

关联：[[13-客户旧包Y8诊断]]、[[00-当前状态]]。
