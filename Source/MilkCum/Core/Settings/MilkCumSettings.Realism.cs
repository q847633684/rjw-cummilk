using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Core.Settings;

/// <summary>拟真子系统 SYS-01～10：默认全开（新安装或设置中无存档键时），SYS-10 泌乳建立除外；已写入 Mod 设置的键仍按保存值读取。见 记忆库/design/拟真子系统开关清单与优先级.md。</summary>
internal partial class MilkCumSettings
{
    /// <summary>SYS-01：非线性顺应性进水（近撑大上限时有效进流降低）。</summary>
    public static bool realismComplianceInflow = true;
    /// <summary>顺应性曲线指数：越大越「晚才变憋」。</summary>
    public static float realismComplianceExponent = 1.35f;
    /// <summary>满撑大时进水倍率下限（相对 1）。</summary>
    public static float realismComplianceFloor = 0.08f;

    /// <summary>SYS-02a：形变缓冲 — 用可调「相对基容」作为单侧物理上限（替代固定 StretchCapFactor 语义下的进水顶格）。</summary>
    public static bool realismStretchBuffer = true;
    /// <summary>相对单侧基容的额外可蓄比例（0.2 → 基容×1.2 与默认 1.2 一致）。</summary>
    public static float realismStretchExtraFraction = 0.2f;

    /// <summary>SYS-02b：RJW 撑大严重度随池伸张按阶梯更新，减少每 tick 微抖。</summary>
    public static bool realismRjwStretchMilestone = true;
    /// <summary>阶梯数（4=t_pool 按 0.25 量化）。</summary>
    public static float realismRjwStretchMilestoneSteps = 4f;

    /// <summary>SYS-02c：RJW 胀大同步按「每叶/每键」郁积度分别算 t_pool（与进水 stretchCap 一致），避免双侧取总限时一侧很胀却 Severity 不抬。</summary>
    public static bool realismRjwStretchPerSideSync = true;

    /// <summary>SYS-03：代谢门控 — 饥饿/口渴时削减进水。</summary>
    public static bool realismMetabolicGate = true;
    public static float realismMetabolicHungerThreshold = 0.25f;
    public static float realismMetabolicThirstThreshold = 0.3f;

    /// <summary>SYS-03：池内自给 — 从乳池扣量补饱食（及 DBH 口渴）。</summary>
    public static bool realismPoolFedSelf = true;
    public static float realismPoolFedSelfFoodTarget = 0.35f;
    /// <summary>每 60tick 最多动用总乳量比例。</summary>
    public static float realismPoolFedSelfMaxPoolFractionPer60 = 0.12f;

    /// <summary>SYS-04：可解释高压漏乳 — 期望由满度/压力（可选炎症）决定，少量随机。</summary>
    public static bool realismReflexLeak = true;
    public static float realismLeakBaseRate = 0.04f;
    public static float realismLeakPressureScale = 0.12f;
    public static float realismLeakFullnessScale = 0.35f;
    public static float realismLeakInflammationScale = 0.2f;
    public static float realismLeakVariance = 0.15f;

    /// <summary>SYS-05：排空–淤积耦合 — 长期高满加重 stasis；近期排空略降 stasis、略抬进水。</summary>
    public static bool realismEmptyStasisCoupling = true;
    public static float realismStasisLongFullScale = 0.35f;
    public static float realismStasisReliefScale = 0.82f;
    public static int realismDrainReliefWindowTicks = 18000;
    public static float realismDrainReliefInflowBoost = 1.07f;

    /// <summary>SYS-06：刺激源 — 婴儿亲喂 vs 机械挤奶对泌乳素微刺激的倍率。</summary>
    public static bool realismStimulusSource = true;
    public static float realismBabyStimulusMult = 1.25f;
    public static float realismMachineStimulusMult = 0.85f;

    /// <summary>SYS-07：疼痛/心情抑制喷乳效率。</summary>
    public static bool realismStressLetdown = true;
    public static float realismStressPainStart = 0.08f;
    public static float realismStressPainMaxDrop = 0.55f;
    public static float realismStressMoodThreshold = 0.2f;
    public static float realismStressMoodMaxDrop = 0.35f;

    /// <summary>SYS-08：断奶慢变 — L 向 Severity 下落时平滑（上冲仍即时）。实验性，与封顶/药品交互复杂。</summary>
    public static bool realismWeaningCurve;
    public static float realismWeaningHalfLifeDays = 1.5f;

    /// <summary>SYS-09：昼夜节律对进水的弱正弦修正。</summary>
    public static bool realismCircadian = true;
    public static float realismCircadianAmplitude = 0.08f;
    public static float realismCircadianPhaseHours = 14f;

    /// <summary>SYS-10：人类泌乳建立 — 自 Lactating 起若干游戏日内进水自低渐增至满倍率；非人形与永久泌乳基因不套用。</summary>
    public static bool realismLactationEstablishment;
    /// <summary>自出现泌乳至进水达 100% 的游戏日数。</summary>
    public static float realismEstablishmentDays = 3f;
    /// <summary>第 0 日进水相对满倍率的下限（与 1 之间线性插值）。</summary>
    public static float realismEstablishmentMinMult = 0.35f;

    internal static void ExposeRealismData()
    {
        Scribe_Values.Look(ref realismComplianceInflow, "MC2.EM.Realism.ComplianceInflow", true);
        Scribe_Values.Look(ref realismComplianceExponent, "MC2.EM.Realism.ComplianceExponent", 1.35f);
        Scribe_Values.Look(ref realismComplianceFloor, "MC2.EM.Realism.ComplianceFloor", 0.08f);
        Scribe_Values.Look(ref realismStretchBuffer, "MC2.EM.Realism.StretchBuffer", true);
        Scribe_Values.Look(ref realismStretchExtraFraction, "MC2.EM.Realism.StretchExtraFraction", 0.2f);
        Scribe_Values.Look(ref realismRjwStretchMilestone, "MC2.EM.Realism.RjwStretchMilestone", false);
        Scribe_Values.Look(ref realismRjwStretchMilestoneSteps, "MC2.EM.Realism.RjwStretchMilestoneSteps", 4f);
        Scribe_Values.Look(ref realismRjwStretchPerSideSync, "MC2.EM.Realism.RjwStretchPerSideSync", true);
        Scribe_Values.Look(ref realismMetabolicGate, "MC2.EM.Realism.MetabolicGate", true);
        Scribe_Values.Look(ref realismMetabolicHungerThreshold, "MC2.EM.Realism.MetabolicHungerThresh", 0.25f);
        Scribe_Values.Look(ref realismMetabolicThirstThreshold, "MC2.EM.Realism.MetabolicThirstThresh", 0.3f);
        Scribe_Values.Look(ref realismPoolFedSelf, "MC2.EM.Realism.PoolFedSelf", true);
        Scribe_Values.Look(ref realismPoolFedSelfFoodTarget, "MC2.EM.Realism.PoolFedFoodTarget", 0.35f);
        Scribe_Values.Look(ref realismPoolFedSelfMaxPoolFractionPer60, "MC2.EM.Realism.PoolFedMaxFrac60", 0.12f);
        Scribe_Values.Look(ref realismReflexLeak, "MC2.EM.Realism.ReflexLeak", true);
        Scribe_Values.Look(ref realismLeakBaseRate, "MC2.EM.Realism.LeakBaseRate", 0.04f);
        Scribe_Values.Look(ref realismLeakPressureScale, "MC2.EM.Realism.LeakPressureScale", 0.12f);
        Scribe_Values.Look(ref realismLeakFullnessScale, "MC2.EM.Realism.LeakFullnessScale", 0.35f);
        Scribe_Values.Look(ref realismLeakInflammationScale, "MC2.EM.Realism.LeakInflammationScale", 0.2f);
        Scribe_Values.Look(ref realismLeakVariance, "MC2.EM.Realism.LeakVariance", 0.15f);
        Scribe_Values.Look(ref realismEmptyStasisCoupling, "MC2.EM.Realism.EmptyStasisCoupling", true);
        Scribe_Values.Look(ref realismStasisLongFullScale, "MC2.EM.Realism.StasisLongFullScale", 0.35f);
        Scribe_Values.Look(ref realismStasisReliefScale, "MC2.EM.Realism.StasisReliefScale", 0.82f);
        Scribe_Values.Look(ref realismDrainReliefWindowTicks, "MC2.EM.Realism.DrainReliefWindowTicks", 18000);
        Scribe_Values.Look(ref realismDrainReliefInflowBoost, "MC2.EM.Realism.DrainReliefInflowBoost", 1.07f);
        Scribe_Values.Look(ref realismStimulusSource, "MC2.EM.Realism.StimulusSource", true);
        Scribe_Values.Look(ref realismBabyStimulusMult, "MC2.EM.Realism.BabyStimulusMult", 1.25f);
        Scribe_Values.Look(ref realismMachineStimulusMult, "MC2.EM.Realism.MachineStimulusMult", 0.85f);
        Scribe_Values.Look(ref realismStressLetdown, "MC2.EM.Realism.StressLetdown", true);
        Scribe_Values.Look(ref realismStressPainStart, "MC2.EM.Realism.StressPainStart", 0.08f);
        Scribe_Values.Look(ref realismStressPainMaxDrop, "MC2.EM.Realism.StressPainMaxDrop", 0.55f);
        Scribe_Values.Look(ref realismStressMoodThreshold, "MC2.EM.Realism.StressMoodThresh", 0.2f);
        Scribe_Values.Look(ref realismStressMoodMaxDrop, "MC2.EM.Realism.StressMoodMaxDrop", 0.35f);
        Scribe_Values.Look(ref realismWeaningCurve, "MC2.EM.Realism.WeaningCurve", true);
        Scribe_Values.Look(ref realismWeaningHalfLifeDays, "MC2.EM.Realism.WeaningHalfLifeDays", 1.5f);
        Scribe_Values.Look(ref realismCircadian, "MC2.EM.Realism.Circadian", true);
        Scribe_Values.Look(ref realismCircadianAmplitude, "MC2.EM.Realism.CircadianAmplitude", 0.08f);
        Scribe_Values.Look(ref realismCircadianPhaseHours, "MC2.EM.Realism.CircadianPhaseHours", 14f);
        Scribe_Values.Look(ref realismLactationEstablishment, "MC2.EM.Realism.LactationEstablishment", false);
        Scribe_Values.Look(ref realismEstablishmentDays, "MC2.EM.Realism.EstablishmentDays", 3f);
        Scribe_Values.Look(ref realismEstablishmentMinMult, "MC2.EM.Realism.EstablishmentMinMult", 0.35f);
    }
}
