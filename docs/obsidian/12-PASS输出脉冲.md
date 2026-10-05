---
title: PASS输出脉冲
status: current
created: 2026-10-05
updated: 2026-10-05
source: 用户要求; Runtime/TestPlans/Rfp7000V2.testplan.json; TestPlanItemExecutor.ExecuteResultOutputAsync
---

# PASS输出脉冲

已验证事实：当前完整测试计划的 result.output 配置为 passPulseOutputChannel=8、passPulseMs=1000。PASS 分支先写 Y8=True，等待 1000 ms，再写 Y8=False；NG 分支不触发 PASS 脉冲。源计划和 Runtime 计划同步修改。

当前 Runtime/Config/AppSettings.json 选择 Hardware 模式和 Rfp7000V2.testplan.json。独立 BurnOnly 计划仍关闭物理结果输出。

StationSafeCleanupOptions.FromTestItems 从结果步骤读取脉冲通道，安全清理尝试将该通道关闭。实际硬件脉宽和接线尚未验证。

相关输出测试和计划同步测试共 3 项通过；两组扩大测试合计 81 项，75 通过、6 失败，失败涉及已有功能组行为和其他计划参数，与本次通道改动无交集。

本次变更仅更换两份计划的通道；未修改执行器。现有未提交执行器已有脉冲实现，但 HEAD 尚无该实现，单独提交配置无法形成完整可恢复版本。因此独立分支保存 docs/pass-y8-1s.patch；在保留当前工作区基线的副本上运行 git apply --ignore-space-change docs/pass-y8-1s.patch 即可恢复此次差异，已修改的副本不重复应用。

关联：[[00-当前状态]]、[[07-关键流程]]。
