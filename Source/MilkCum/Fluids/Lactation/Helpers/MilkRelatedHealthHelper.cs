using MilkCum.Core;
using RimWorld;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 泌乳相关健康状态更新：胀满、炎症、乳腺炎（炎症触发与 MTB 触发）�?
/// 主流程只调本 Helper 的入口，逻辑集中在此，避�?CompEquallyMilkable 膨胀。见 记忆�?docs/待办清单 二、代码位置与主流程�?
/// </summary>
public static class MilkRelatedHealthHelper
{
    /// <summary>满池时添加「乳房胀满」hediff；低�?90% maxFullness 时移除（滞后避免抖动）。由 CompTick �?tick 调用</summary>
    public static void UpdateBreastsEngorged(Pawn pawn, float fullness, float maxFullness)
    {
        if (MilkCumDefOf.EM_BreastsEngorged == null || pawn == null || !pawn.RaceProps.Humanlike || !pawn.IsLactating()) return;
        var engorged = pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_BreastsEngorged);
        float fullThreshold = 0.95f * maxFullness;
        float lowThreshold = 0.9f * maxFullness;
        if (fullness >= fullThreshold && engorged == null)
            pawn.health.AddHediff(MilkCumDefOf.EM_BreastsEngorged, pawn.GetBreastOrChestPart());
        else if (fullness < lowThreshold && engorged != null)
            pawn.health.RemoveHediff(engorged);
    }

    /// <summary>四层模型：更新炎�?I 并尝试按 I&gt;I_crit 触发乳腺炎。由 UpdateMilkPools �?60 tick 调用</summary>
    public static void UpdateInflammationAndTryTriggerMastitis(HediffComp_EqualMilkingLactating lactatingComp, float fullness, float maxFullness)
    {
        if (!MilkCumSettings.enableInflammationModel || lactatingComp == null) return;
        float maxF = UnityEngine.Mathf.Max(0.001f, maxFullness);
        lactatingComp.UpdateInflammation(fullness / maxF, 60f / 3600f);
        lactatingComp.TryTriggerMastitisFromInflammation();
    }

    /// <summary>规格：乳腺炎/堵塞可由长时间满池、卫生、受伤等触发。每 2000 tick 判定一次；参数�?MilkCumSettings 可调</summary>
    public static void TryTriggerMastitisFromMtb(Pawn pawn, float fullness, int ticksFullPool)
    {
        if (MilkCumDefOf.EM_Mastitis == null || pawn == null || !pawn.RaceProps.Humanlike || !pawn.IsLactating()) return;
        if (!MilkCumSettings.allowMastitis) return;
        bool longFull = ticksFullPool >= 60000;
        float hygieneRisk = DubsBadHygieneIntegration.GetHygieneRiskFactorForMastitis(pawn);
        bool badHygiene = hygieneRisk >= 0.4f;
        bool torsoInjury = HasTorsoOrBreastInjury(pawn);
        if (!longFull && !badHygiene && !torsoInjury) return;
        float mtbDays = MilkCumSettings.mastitisBaseMtbDays;
        if (mtbDays < 0.1f) mtbDays = 0.1f;
        if (longFull) mtbDays /= UnityEngine.Mathf.Max(0.1f, MilkCumSettings.overFullnessRiskMultiplier);
        if (badHygiene) mtbDays /= UnityEngine.Mathf.Max(0.1f, (0.5f + hygieneRisk) * MilkCumSettings.hygieneRiskMultiplier);
        if (torsoInjury) mtbDays /= 1.3f;
        float nutritionFactor = 0.5f + 0.5f * UnityEngine.Mathf.Clamp(PawnUtility.BodyResourceGrowthSpeed(pawn), 0f, 1f);
        mtbDays *= UnityEngine.Mathf.Max(0.3f, nutritionFactor);
        float raceMultiplier = pawn.RaceProps.Humanlike ? MilkCumSettings.mastitisMtbDaysMultiplierHumanlike : MilkCumSettings.mastitisMtbDaysMultiplierAnimal;
        mtbDays *= UnityEngine.Mathf.Max(0.01f, raceMultiplier);
        if (!Rand.MTBEventOccurs(mtbDays, 60000f, 2000f)) return;
        var existing = pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_Mastitis);
        if (existing != null)
        {
            if (existing.Severity < 0.99f)
                existing.Severity = UnityEngine.Mathf.Min(1f, existing.Severity + 0.15f);
        }
        else
            pawn.health.AddHediff(MilkCumDefOf.EM_Mastitis, pawn.GetBreastOrChestPart());
    }

    /// <summary>是否有躯�?乳房损伤，用于乳腺炎 MTB 风险判定</summary>
    public static bool HasTorsoOrBreastInjury(Pawn pawn)
    {
        if (pawn?.health?.hediffSet?.hediffs == null) return false;
        foreach (var h in pawn.health.hediffSet.hediffs)
        {
            if (h.Part?.def?.defName == null) continue;
            string dn = h.Part.def.defName;
            if (dn.StartsWith("Torso") || dn.StartsWith("Breast") || dn.StartsWith("Chest")) return true;
        }
        return false;
    }
}
