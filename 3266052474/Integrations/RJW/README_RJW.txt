RJW 集成说明
=============
本目录仅在启用 RJW (rim.job.world) 时由 LoadFolders 加载。

已实现
------
• Harmony：ExtensionHelper.MilkAmount 按 RJW 胸部 FluidMultiplier 放大产奶量。
• Harmony：ExtensionHelper.MilkDef 优先使用 RJW 胸部 Fluid 的 consumable 作为奶产物。
• Hediff_BasePregnancy.PostBirth：分娩后为可挤奶母亲添加/刷新 Lactating（本 mod 的泌乳期），并允许哺乳。
• Patches/RJWLactatingGainPatch.xml：为 Lactating_Drug、Lactating_Permanent、Heavy_Lactating_Permanent 添加意识/操纵/移动 +10% 增益（与本体泌乳期一致）。这些 def 多来自 rjw-milkable-colonists 等子 mod；若未安装则 xpath 不匹配、操作跳过。
• Alert_FluidMultiplier：当有小人胸部流体倍率为 0 时提醒用 RJW 编辑部件修复。

代码可改进点（可选）
------------------
• RJWVersionDiffHelper.GetBreasts：1.6 分支若 GetLewdParts() 或 Breasts 为 null 可能 NRE，可加空合并返回 Enumerable.Empty<ISexPartHediff>()。
• RJW.cs 中 PostBirth 已对 mother.IsMilkable() / IsLactating() / CompEquallyMilkable() 做判断，逻辑清晰；若 RJW 后续 API 变更再适配。

其他 RJW 支持建议
----------------
• 若 Steam 版 rjw 将来启用 Hediffs_Lactating 中的 RJW_lactating，可在此目录增加一条 PatchOperationAdd，为其 stages 添加相同 capMods，与 Lactating_Drug 一致。
• 在 mod 描述或 README 中注明：与 rim.job.world 兼容；与 rjw.milk.humanoid / Mlie.MilkableColonists 不兼容，避免与“可挤奶殖民者”类 mod 冲突。
• 可选：在设置或统计界面当检测到 RJW 激活时，简短提示“产奶量会受 RJW 胸部流体倍率影响”。
