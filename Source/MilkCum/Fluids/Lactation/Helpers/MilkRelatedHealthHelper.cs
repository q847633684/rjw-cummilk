using System.Collections.Generic;
using MilkCum.Core;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 泌乳相关健康：乳房胀满、奶水瘀积、乳腺炎、乳房化脓；炎症 I 与 MTB；Lactating 维护。
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

    /// <summary>任一乳池条目满度 ≥ 基础容量×满阈时添加胀满；当所有池条目都 &lt; 0.9×基础容量时移除。</summary>
    public static void UpdateBreastsEngorged(Pawn pawn, CompEquallyMilkable comp)
    {
        if (MilkCumDefOf.EM_BreastsEngorged == null || pawn == null || !pawn.RaceProps.Humanlike || !pawn.IsLactating()) return;
        if (comp == null) return;
        var entries = comp.GetResolvedBreastPoolEntries();
        if (entries.Count == 0) return;
        float th = PoolModelConstants.FullnessThresholdFactor;
        bool anyAtEngorgedLevel = false;
        bool allSidesUnderRelief = true;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            float cur = comp.GetFullnessForKey(e.Key);
            if (cur >= e.Capacity * th)
                anyAtEngorgedLevel = true;
            if (cur >= 0.9f * e.Capacity)
                allSidesUnderRelief = false;
        }
        var engorged = pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_BreastsEngorged);
        if (anyAtEngorgedLevel && engorged == null)
            pawn.health.AddHediff(MilkCumDefOf.EM_BreastsEngorged, pawn.GetBreastOrChestPart());
        else if (allSidesUnderRelief && engorged != null)
            pawn.health.RemoveHediff(engorged);
    }

    /// <summary>奶水瘀积：各侧满池 tick 取最大参与久满判定；缓解要求每一侧相对撑大容量比例均 &lt; 0.56。</summary>
    public static void UpdateLactationalMilkStasis(Pawn pawn, CompEquallyMilkable comp)
    {
        if (MilkCumDefOf.EM_LactationalMilkStasis == null || pawn == null || !pawn.RaceProps.Humanlike || !pawn.IsLactating()) return;
        if (!MilkCumSettings.allowMastitis) return;
        if (comp == null) return;
        if (pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_Mastitis) != null
            || (MilkCumDefOf.EM_BreastAbscess != null && pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_BreastAbscess) != null))
        {
            RemoveLactationalMilkStasis(pawn);
            return;
        }
        var entries = comp.GetResolvedBreastPoolEntries();
        if (entries.Count == 0) return;
        int maxTicks = 0;
        bool allSidesPoolLow = true;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            int t = comp.GetTicksFullPoolForKey(e.Key);
            if (t > maxTicks) maxTicks = t;
            float stretch = PoolModelConstants.CapacityStretchCapMin001(e.Capacity);
            float cur = comp.GetFullnessForKey(e.Key);
            if (cur / stretch >= 0.56f)
                allSidesPoolLow = false;
        }
        float iMax = pawn.LactatingHediffComp()?.CurrentInflammation ?? 0f;
        float crit = Mathf.Max(0.01f, MilkCumSettings.inflammationCrit);
        bool inflamBand = MilkCumSettings.enableInflammationModel && iMax >= crit * 0.38f;
        bool engorged = MilkCumDefOf.EM_BreastsEngorged != null
                        && pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_BreastsEngorged) != null;
        bool longStasisWindow = maxTicks >= 16000;
        bool engorgedAndBuilding = engorged && maxTicks >= 7000;
        var stasis = pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_LactationalMilkStasis);
        if (stasis == null && (longStasisWindow || engorgedAndBuilding || inflamBand))
            pawn.health.AddHediff(MilkCumDefOf.EM_LactationalMilkStasis, pawn.GetBreastOrChestPart());
        else if (stasis != null)
        {
            bool lowI = !MilkCumSettings.enableInflammationModel || iMax < crit * 0.28f;
            if (allSidesPoolLow && lowI)
                pawn.health.RemoveHediff(stasis);
        }
    }

    public static void RemoveLactationalMilkStasis(Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null || MilkCumDefOf.EM_LactationalMilkStasis == null) return;
        var h = pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_LactationalMilkStasis);
        if (h != null) pawn.health.RemoveHediff(h);
    }

    /// <summary>更新炎症 I 并尝试 I 阈值触发乳腺炎。</summary>
    public static void UpdateInflammationAndTryTriggerMastitis(HediffComp_EqualMilkingLactating lactatingComp)
    {
        if (!MilkCumSettings.enableInflammationModel || lactatingComp == null) return;
        var milkComp = lactatingComp.CompEquallyMilkable;
        if (milkComp == null) return;
        float stasisScale = MilkRealismHelper.GetStasisTermScale(milkComp);
        lactatingComp.UpdateInflammation(milkComp, 60f / 3600f, stasisScale);
        lactatingComp.TryTriggerMastitisFromInflammation();
    }

    /// <summary>MTB 乳腺炎：任一侧「满池计数 ≥1 天」单独过 MTB；卫生/伤仍全局一次，避免多侧重复乘概率。</summary>
    public static void TryTriggerMastitisFromMtb(Pawn pawn, CompEquallyMilkable comp)
    {
        if (MilkCumDefOf.EM_Mastitis == null || pawn == null || !pawn.RaceProps.Humanlike || !pawn.IsLactating()) return;
        if (!MilkCumSettings.allowMastitis) return;
        if (comp == null) return;
        if (MilkCumDefOf.EM_BreastAbscess != null && pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_BreastAbscess) != null) return;
        float hygieneRisk = DubsBadHygieneIntegration.GetHygieneRiskFactorForMastitis(pawn);
        bool badHygiene = hygieneRisk >= 0.4f;
        bool torsoInjury = HasTorsoOrBreastInjury(pawn);
        var entries = comp.GetResolvedBreastPoolEntries();
        if (entries.Count == 0) return;
        int dayTicks = (int)PoolModelConstants.TicksPerGameDay;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            if (comp.GetTicksFullPoolForKey(e.Key) < dayTicks) continue;
            float mtbDays = ComputeMastitisMtbDays(true, hygieneRisk, badHygiene, torsoInjury, pawn);
            if (!Rand.MTBEventOccurs(mtbDays, PoolModelConstants.TicksPerGameDay, 2000f)) continue;
            ApplyMastitisFromMtb(pawn);
            return;
        }
        if (!badHygiene && !torsoInjury) return;
        float mtbDaysGlobal = ComputeMastitisMtbDays(false, hygieneRisk, badHygiene, torsoInjury, pawn);
        if (!Rand.MTBEventOccurs(mtbDaysGlobal, PoolModelConstants.TicksPerGameDay, 2000f)) return;
        ApplyMastitisFromMtb(pawn);
    }

    private static float ComputeMastitisMtbDays(bool fullnessRisk, float hygieneRisk, bool badHygiene, bool torsoInjury, Pawn pawn)
    {
        float mtbDays = MilkCumSettings.mastitisBaseMtbDays;
        if (mtbDays < 0.1f) mtbDays = 0.1f;
        if (fullnessRisk)
            mtbDays /= Mathf.Max(0.1f, MilkCumSettings.overFullnessRiskMultiplier);
        if (badHygiene)
            mtbDays /= Mathf.Max(0.1f, (0.5f + hygieneRisk) * MilkCumSettings.hygieneRiskMultiplier);
        if (torsoInjury)
            mtbDays /= 1.3f;
        if (badHygiene && (fullnessRisk || torsoInjury))
            mtbDays /= Mathf.Max(0.1f, MilkCumSettings.mastitisInfectionRiskFactor);
        float nutritionFactor = 0.5f + 0.5f * Mathf.Clamp(PawnUtility.BodyResourceGrowthSpeed(pawn), 0f, 1f);
        mtbDays *= Mathf.Max(0.3f, nutritionFactor);
        float raceMultiplier = pawn.RaceProps.Humanlike ? MilkCumSettings.mastitisMtbDaysMultiplierHumanlike : MilkCumSettings.mastitisMtbDaysMultiplierAnimal;
        mtbDays *= Mathf.Max(0.01f, raceMultiplier);
        return mtbDays;
    }

    private static void ApplyMastitisFromMtb(Pawn pawn)
    {
        RemoveLactationalMilkStasis(pawn);
        var existing = pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_Mastitis);
        if (existing != null)
        {
            if (existing.Severity < 0.99f)
                existing.Severity = Mathf.Min(1f, existing.Severity + 0.15f);
        }
        else
            pawn.health.AddHediff(MilkCumDefOf.EM_Mastitis, pawn.GetBreastOrChestPart());
    }

    /// <summary>重度乳腺炎在卫生差且（任一侧久满或伤）时 MTB 可进展为乳房化脓。</summary>
    public static void TryTriggerBreastAbscessFromMtb(Pawn pawn, CompEquallyMilkable comp)
    {
        if (MilkCumDefOf.EM_BreastAbscess == null || MilkCumDefOf.EM_Mastitis == null || pawn == null || !pawn.RaceProps.Humanlike || !pawn.IsLactating()) return;
        if (!MilkCumSettings.allowMastitis) return;
        if (comp == null) return;
        if (pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_BreastAbscess) != null) return;
        var mastitis = pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_Mastitis);
        if (mastitis == null || mastitis.Severity < 0.66f) return;
        int dayTicks = (int)PoolModelConstants.TicksPerGameDay;
        bool anySideLongFull = false;
        foreach (var e in comp.GetResolvedBreastPoolEntries())
        {
            if (string.IsNullOrEmpty(e.Key)) continue;
            if (comp.GetTicksFullPoolForKey(e.Key) >= dayTicks)
            {
                anySideLongFull = true;
                break;
            }
        }
        float hygieneRisk = DubsBadHygieneIntegration.GetHygieneRiskFactorForMastitis(pawn);
        bool badHygiene = hygieneRisk >= 0.4f;
        bool torsoInjury = HasTorsoOrBreastInjury(pawn);
        if (!badHygiene || (!anySideLongFull && !torsoInjury)) return;
        float mtbDays = 11f;
        mtbDays /= Mathf.Max(0.1f, MilkCumSettings.mastitisInfectionRiskFactor);
        if (!Rand.MTBEventOccurs(mtbDays, PoolModelConstants.TicksPerGameDay, 2000f)) return;
        pawn.health.RemoveHediff(mastitis);
        RemoveLactationalMilkStasis(pawn);
        pawn.health.AddHediff(MilkCumDefOf.EM_BreastAbscess, pawn.GetBreastOrChestPart());
    }

    /// <summary>挤奶结束：按侧与全局略降瘀积/乳腺炎/化脓严重度（与排空缓解 Comp 叠加）。</summary>
    public static void ApplyMilkingPhysicalRelief(Pawn pawn, ICollection<string> drainedKeys)
    {
        if (pawn?.health?.hediffSet == null) return;
        var hed = pawn.health.hediffSet.hediffs;
        void BumpDown(Hediff h, float amt)
        {
            if (h == null || amt <= 0f) return;
            h.Severity = Mathf.Max(0f, h.Severity - amt);
            if (h.Severity <= 0f) pawn.health.RemoveHediff(h);
        }
        bool Match(Hediff h, HediffDef d, BodyPartRecord part) => h.def == d && h.Part == part;

        if (drainedKeys != null && drainedKeys.Count > 0)
        {
            var partsDone = new HashSet<BodyPartRecord>();
            foreach (string sk in drainedKeys)
            {
                if (string.IsNullOrEmpty(sk)) continue;
                BodyPartRecord part = pawn.GetPartForPoolKey(sk);
                if (part == null || !partsDone.Add(part)) continue;
                for (int i = 0; i < hed.Count; i++)
                {
                    var hi = hed[i];
                    if (MilkCumDefOf.EM_Mastitis != null && Match(hi, MilkCumDefOf.EM_Mastitis, part)) BumpDown(hi, 0.02f);
                    if (MilkCumDefOf.EM_BreastAbscess != null && Match(hi, MilkCumDefOf.EM_BreastAbscess, part)) BumpDown(hi, 0.015f);
                    if (MilkCumDefOf.EM_LactationalMilkStasis != null && Match(hi, MilkCumDefOf.EM_LactationalMilkStasis, part)) BumpDown(hi, 0.012f);
                }
            }
        }
        if (MilkCumDefOf.EM_Mastitis != null)
            BumpDown(pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_Mastitis), 0.05f);
        if (MilkCumDefOf.EM_BreastAbscess != null)
            BumpDown(pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_BreastAbscess), 0.04f);
        if (MilkCumDefOf.EM_LactationalMilkStasis != null)
            BumpDown(pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_LactationalMilkStasis), 0.025f);
    }

    /// <summary>是否有躯干/乳房损伤（乳腺炎 MTB 风险）</summary>
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
