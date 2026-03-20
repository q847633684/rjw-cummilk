using MilkCum.Core;
using MilkCum.Core.Constants;
using MilkCum.Fluids.Lactation.Comps;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 泌乳相关健康状态更新：胀满、炎症、乳腺炎（炎症触发与 MTB 触发）、Lactating hediff 维护。
/// 主流程只调本 Helper 的入口，逻辑集中在此，避免 CompEquallyMilkable 膨胀。
/// </summary>
public static class MilkRelatedHealthHelper
{
    /// <summary>根据基因/物种/设置维护 Lactating Hediff（增删与 Severity）。由 CompTick 每 120 tick 调用。</summary>
    public static void EnsureLactatingHediffFromConditions(Pawn pawn)
    {
        if (pawn == null || !pawn.SpawnedOrAnyParentSpawned || !pawn.IsColonyPawn() || pawn.Faction == null)
            return;
        if (pawn.genes?.HasActiveGene(MilkCumDefOf.EM_Permanent_Lactation) == true)
        {
            Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating, pawn.GetBreastOrChestPart());
            lactating.Severity = Mathf.Max(lactating.Severity, 0.9999f);
            pawn.CompEquallyMilkable()?.SetEntriesCacheDirty();
            MilkCumSettings.LactationLog($"Lactating ensured (permanent gene): {pawn.Name}");
            return;
        }
        if (!pawn.RaceProps.Humanlike && !pawn.IsMilkable())
        {
            if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating) is Hediff lactating)
            {
                MilkCumSettings.LactationLog($"Lactating removed (not milkable): {pawn.Name}");
                pawn.health.RemoveHediff(lactating);
                pawn.CompEquallyMilkable()?.SetEntriesCacheDirty();
            }
            return;
        }
        if (MilkCumSettings.femaleAnimalAdultAlwaysLactating && pawn.IsAdultFemaleAnimalOfColony())
        {
            Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating, pawn.GetBreastOrChestPart());
            lactating.Severity = Mathf.Max(lactating.Severity, 1f);
            pawn.CompEquallyMilkable()?.SetEntriesCacheDirty();
            MilkCumSettings.LactationLog($"Lactating ensured (animal always): {pawn.Name}");
        }
    }

    /// <summary>满池时添加「乳房胀满」hediff；低于 90% maxFullness 时移除（滞后避免抖动）。由 CompTick 每 tick 调用</summary>
    public static void UpdateBreastsEngorged(Pawn pawn, float fullness, float maxFullness)
    {
        if (MilkCumDefOf.EM_BreastsEngorged == null || pawn == null || !pawn.RaceProps.Humanlike || !pawn.IsLactating()) return;
        var engorged = pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_BreastsEngorged);
        float fullThreshold = PoolModelConstants.FullnessThresholdFactor * maxFullness;
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
        bool longFull = ticksFullPool >= (int)PoolModelConstants.TicksPerGameDay;
        float hygieneRisk = DubsBadHygieneIntegration.GetHygieneRiskFactorForMastitis(pawn);
        bool badHygiene = hygieneRisk >= 0.4f;
        bool torsoInjury = HasTorsoOrBreastInjury(pawn);
        if (!longFull && !badHygiene && !torsoInjury) return;
        float mtbDays = MilkCumSettings.mastitisBaseMtbDays;
        if (mtbDays < 0.1f) mtbDays = 0.1f;
        if (longFull) mtbDays /= UnityEngine.Mathf.Max(0.1f, MilkCumSettings.overFullnessRiskMultiplier);
        if (badHygiene) mtbDays /= UnityEngine.Mathf.Max(0.1f, (0.5f + hygieneRisk) * MilkCumSettings.hygieneRiskMultiplier);
        if (torsoInjury) mtbDays /= 1.3f;
        // 医学贴近：卫生差且（淤积或损伤）时感染风险升高，MTB 再降（对应细菌经破损/不洁侵入）
        if (badHygiene && (longFull || torsoInjury))
            mtbDays /= UnityEngine.Mathf.Max(0.1f, MilkCumSettings.mastitisInfectionRiskFactor);
        float nutritionFactor = 0.5f + 0.5f * UnityEngine.Mathf.Clamp(PawnUtility.BodyResourceGrowthSpeed(pawn), 0f, 1f);
        mtbDays *= UnityEngine.Mathf.Max(0.3f, nutritionFactor);
        float raceMultiplier = pawn.RaceProps.Humanlike ? MilkCumSettings.mastitisMtbDaysMultiplierHumanlike : MilkCumSettings.mastitisMtbDaysMultiplierAnimal;
        mtbDays *= UnityEngine.Mathf.Max(0.01f, raceMultiplier);
        if (!Rand.MTBEventOccurs(mtbDays, PoolModelConstants.TicksPerGameDay, 2000f)) return;
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
