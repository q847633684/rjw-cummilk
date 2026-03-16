# -*- coding: utf-8 -*-
"""Batch replace mojibake comments with correct Chinese in MilkCumSettings.cs.
Line-based: if a line contains the key phrase and is garbled, replace entire line.
"""
import os
import re

path = os.path.join(os.path.dirname(__file__), "Source", "MilkCum", "Core", "Settings", "MilkCumSettings.cs")

# (key_phrase_in_line, full_correct_line_content no leading tab/newline)
LINE_REPLACEMENTS = [
    ("baseFlowPerSecond = 60/", "\t/// <summary>挤奶流速基准：baseFlowPerSecond = 60/基准值（池单位/秒）。默认 60 → 满池约 1 瓶/秒（现实时间）；调大则变慢。</summary>"),
    ("effectiveTime *= (1 + ", "\t/// <summary>按容量量化：吸奶有效时间随喂奶量 MilkAmount 的系数，effectiveTime *= (1 + 系数*(MilkAmount-1))，限制在 [0.5, 2]。</summary>"),
    ("lactationExtraNutritionBasis = 150", "\t/// <summary>泌乳期满期间额外进食：滑杆 0～100，150=1:1。饱食度每 150 tick 额下降 = flowPerDay×(150/60000)×(基准/150)。</summary>"),
    ("reabsorbNutritionEnabled = true", "\t/// <summary>回缩吸收：满池回缩时，未溢出部分视为被身体吸收，按比例补充饱食度；0=关闭，1=与产奶消耗 1:1 折算。</summary>"),
    ("milkingActionLog = false", "\t/// <summary>DevMode 且勾选时，输出吸奶/挤奶/机器产奶入口汇总日志（每次操作一组），用于平衡与 AI 调试。</summary>"),
    ("lactationLog = true", "\t/// <summary>DevMode 时勾选则输出泌乳关键路径日志（分泌、进水、移除奶等）；勾选可减少刷屏，仅用 PoolTickLog 看明细。</summary>"),
    ("lactationDrugIntakeLog = false", "\t/// <summary>勾选时，每次吃药进水（AddFromDrug）时输出调试日志：L、进水、剩余时间变化。</summary>"),
    ("lactationLog 寮€鍏虫帶鍒躲€", "\t/// <summary>DevMode 时输出泌乳关键路径日志，便于排查 L/池/药物/分娩 行为。受 lactationLog 开关控制。</summary>"),
    ("lactationPoolTickLog 涓?true 鏃惰緭鍑猴", "\t/// <summary>仅当 DevMode 且 lactationPoolTickLog 为 true 时输出，用于每步营养/奶池/回缩/吸奶明细。</summary>"),
    ("rjwBreastCapacityCoefficient = 2f", "\t/// <summary>乳房容量系数：左右平衡容量 = RJW Severity × 该系数，2=默认，与泌乳效率等可调项对应。</summary>"),
    ("rjwLactationFertilityFactor = 0.85f", "\t/// <summary>泌乳期怀孕概率乘数(0~1)</summary>"),
    ("rjwSexAddsLactationBoost = false", "\t/// <summary>3.2：性行为后为泌乳参与者增加少量池进水（可选），可选。</summary>"),
    ("useDubsBadHygieneForMastitis = true", "\t// 乳腺炎触发：卫生触发是否与 Dubs Bad Hygiene 联动（有 DBH 时用 Hygiene 需求，否则用房间清洁度）。"),
    ("allowToleranceAffectMilk ", "\t// 耐受对泌乳效率的影响：关闭则 E_tol 恒为 1；指数控制曲线（1=线性）"),
    ("_risk ??= new MilkRiskSettings", "\t// 建议 13：收纳为 MilkRiskSettings，便于序列化与 UI 分组；对外仍用静态属性，存盘时带 key"),
    ("enableToleranceDynamic = true", "\t// 耐受动态：dE/dt = μ×L − ν×E；启用时由 mod 维护的 E 计算 E_tol（流速/容量），取代仅用游戏内耐受严重度 t。"),
    ("toleranceDynamicMu = 0.03f", "\t/// <summary>耐受累积率 μ（每游戏日），高则 E 上升。</summary>"),
    ("toleranceDynamicNu = 0.08f", "\t/// <summary>耐受衰减率 ν（每游戏日），自然回落。</summary>"),
    ("ProlactinToleranceGainPerDose = 0.044f", "\t/// <summary>催乳素单剂在 XML 中对耐受 Hediff 的 Severity 增量（与 Lactating 同剂叠加一致，默认 0.044）；改 XML 时需同步。</summary>"),
    ("baselineMilkDurationDays 鍙嶆帹", "\t/// <summary>药物泌乳容量用有效数 B_T：由 baselineMilkDurationDays 反推，使单次剂量（L≥0.5、E=1）时剩余天数 ≥ 基准天数。B=0.5/baseline ⇒ B_T_eff=1/(0.5/baseline−k×0.5)。</summary>"),
    ("birthInducedMilkDurationDays 鍙嶆帹", "\t/// <summary>分娩泌乳容量用有效数 B_T：由 birthInducedMilkDurationDays 反推，公式同药物。</summary>"),
    ("GetPressureFactor(float P)", "\t\t// 带下限 Logistic：最小 f_min，P 越大越接近 f_min"),
    ("aiPreferHighFullnessTargets = true", "\t// 挤奶工作：是否优先选择满度更高的目标（殖民者会先挤更满的）"),
    ("enableLetdownReflex = true", "\t// 四层模型（阶段）：催产反射 R。启用时流速乘 R；每 60 tick 指数衰减，挤奶/吸奶时升高。"),
    ("letdownReflexStimulusDeltaR = 0.45f", "\t/// <summary>挤奶/吸奶时 R 的增量 ΔR，R 加上后 Clamp 到 1。略大一些（如 1）可一次刺激即满 R。</summary>"),
    ("milkingLStimulusPerEvent = 0.03f", "\t// 四层模型（阶段）：挤奶/吸奶时 L 幅度刺激（带上限，防无限循环）。"),
    ("milkingLStimulusCapPerDay = 0.2f", "\t// 四层模型（阶段）：挤奶/吸奶时 L 幅度刺激（带上限），由 enableInflammationModel 生效。"),
    ("enableFullPoolLetter = true", "\t// 3.3 满池事件：满池过久（约 1 天）时是否发信提醒？"),
    ("E_tol(t) = max(1 鈭?t, 0.05)", "\t/// <summary>统一耐受系数：E_tol(t) = max(1 − t, 0.05)。启用耐受动态时由 comp 的 E 计算。</summary>"),
    ("E_tol = [max(1鈭扙, 0.05)]^exponent", "\t/// <summary>耐受动态：由 mod 维护的 E 得到 E_tol = [max(1−E, 0.05)]^exponent。</summary>"),
    ("allowToleranceAffectMilk 鍏抽棴鏃舵亽涓?1", "\t/// <summary>统一耐受系数（按严重度 t）：E_tol(t) = [max(1 − t, 0.05)]^exponent；allowToleranceAffectMilk 关闭时为 1。</summary>"),
]

def is_garbled(s):
    """Rough: line contains mojibake-like chars."""
    return "鎸" in s or "鍥" in s or "鑰愬彈" in s or "娉屼钩" in s or "閸" in s or "鏈" in s and "€" in s

def main():
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    count = 0
    for i, line in enumerate(lines):
        for key, replacement in LINE_REPLACEMENTS:
            if key in line and is_garbled(line):
                # Preserve line ending
                new_line = replacement.rstrip() + "\n" if line.endswith("\n") else replacement.rstrip()
                if lines[i] != new_line:
                    lines[i] = new_line
                    count += 1
                break
    with open(path, "w", encoding="utf-8") as f:
        f.writelines(lines)
    print(f"Replaced {count} lines in {path}")

if __name__ == "__main__":
    main()
