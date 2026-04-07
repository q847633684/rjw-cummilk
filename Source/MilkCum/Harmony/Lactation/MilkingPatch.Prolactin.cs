using System;
using System.Reflection;
using HarmonyLib;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Lactation.World;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Harmony;

/// <summary>设计原则 1：耐受/成瘾交给原版。本补丁仅挂接服用催乳剂后的「水池模型」逻辑。</summary>
public static class ProlactinAddictionPatch
{
    public static void ApplyIfPossible(HarmonyLib.Harmony harmony)
    {
        try
        {
            var postfix = typeof(ProlactinAddictionPatch).GetMethod(nameof(DoIngestionOutcome_Postfix), BindingFlags.Public | BindingFlags.Static);
            if (postfix == null) return;
            var method = AccessTools.Method(typeof(IngestionOutcomeDoer), nameof(IngestionOutcomeDoer.DoIngestionOutcome),
                new[] { typeof(Pawn), typeof(Thing), typeof(int) });
            if (method == null)
                return;
            harmony.Patch(method, postfix: new HarmonyLib.HarmonyMethod(postfix));
        }
        catch (Exception ex)
        {
            Verse.Log.Warning($"[EqualMilking] ProlactinAddictionPatch.ApplyIfPossible failed (ingestion postfix skipped): {ex.Message}");
        }
    }

    public static void DoIngestionOutcome_Postfix(IngestionOutcomeDoer __instance, Pawn pawn, Thing ingested, int ingestedCount)
    {
        if (__instance is not IngestionOutcomeDoer_GiveHediff giveHediff || giveHediff.hediffDef != HediffDefOf.Lactating || pawn?.health?.hediffSet == null)
            return;
        var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating) as HediffWithComps;
        if (hediff == null)
            return;
        float tBefore = Mathf.Max(0f, MilkCumSettings.GetProlactinTolerance(pawn) - MilkCumSettings.ProlactinToleranceGainPerDose);
        float rawSeverity = giveHediff.severity;
        float effectiveSeverity = rawSeverity * MilkCumSettings.GetProlactinToleranceFactor(tBefore);

        if (hediff.Severity > rawSeverity)
        {
            float raceMult = MilkCumSettings.GetRaceDrugDeltaSMultiplier(pawn);
            float deltaS = effectiveSeverity * raceMult;
            float remainingBefore = 0f;
            float lactationBefore = 0f;
            bool logIntake = MilkCumSettings.lactationDrugIntakeLog;

            if (hediff.comps != null)
            {
                foreach (var c in hediff.comps)
                {
                    if (c is HediffComp_EqualMilkingLactating comp)
                    {
                        if (logIntake)
                        {
                            remainingBefore = comp.RemainingDays;
                            lactationBefore = comp.CurrentLactationAmount;
                            comp.SuppressDrugIntakeLog = true;
                        }

                        bool wasMerged = comp.MergedFromIngestionThisTick;
                        float mergedSeverity = comp.LastMergedOtherSeverity;

                        if (comp.MergedFromIngestionThisTick)
                        {
                            MilkCumSettings.LactationLog($"[MilkCum.验证] 已泌乳再次服药 数据: Pawn={pawn?.LabelShort} Def_raw={rawSeverity:F3} 合并other.Severity={mergedSeverity:F3} 自算effective={effectiveSeverity:F3}");
                            MilkCumSettings.LactationLog("[MilkCum.验证] 解读: 原版已乘耐受，合并值≈effective，无需补差");
                            comp.LastMergedOtherSeverity = 0f;
                        }
                        else
                        {
                            comp.AddFromDrug(deltaS);
                        }

                        comp.MergedFromIngestionThisTick = false;
                        if (logIntake)
                        {
                            comp.SuppressDrugIntakeLog = false;
                            float remainingAfter = comp.RemainingDays;
                            float lactationAfter = comp.CurrentLactationAmount;
                            float eTol = MilkCumSettings.GetProlactinToleranceFactor(pawn);
                            float rawInferred = (eTol * raceMult > 1E-6f) ? (deltaS / (eTol * raceMult)) : 0f;
                            float totalDeltaL = wasMerged ? (mergedSeverity * PoolModelConstants.DoseToLFactor) : (lactationAfter - lactationBefore);
                            float deltaRemaining = remainingAfter - remainingBefore;
                            int tick = Find.TickManager.TicksGame;
                            MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] pawn={pawn?.LabelShort} tick={tick} state=AlreadyLactating merged={(wasMerged ? "ByVanillaMerge" : "Direct")}");
                            MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] input rawDef~={rawInferred:F3} Δs={deltaS:F3} E_tol={eTol:F3} raceMult={raceMult:F3} doseToL={PoolModelConstants.DoseToLFactor:F2}");
                            MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] result 本针合计ΔL={totalDeltaL:F3} remainBefore={remainingBefore:F1}d remainAfter={remainingAfter:F1}d Δremain={deltaRemaining:+0.0;-0.0;0.0}d");
                            if (wasMerged)
                                MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] 公式 合并加L={mergedSeverity:F3}×C_dose={mergedSeverity * PoolModelConstants.DoseToLFactor:F3}=合计ΔL；effective=raw({rawSeverity:F3})×E_tol({eTol:F3})={effectiveSeverity:F3} Δs=effective×种族({raceMult:F3})={deltaS:F3}；剩余={remainingBefore:F1}d+Δ天数({deltaRemaining:+0.0;-0.0;0.0})d={remainingAfter:F1}d");
                            else
                                MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] 公式 有效剂量effective=raw({rawSeverity:F3})×E_tol({eTol:F3})={effectiveSeverity:F3}；Δs=effective×种族({raceMult:F3})={deltaS:F3}；ΔL=Δs×C_dose={totalDeltaL:F3}；剩余={remainingBefore:F1}d+Δ天数({deltaRemaining:+0.0;-0.0;0.0})d={remainingAfter:F1}d");
                        }
                        break;
                    }
                }
            }
            WorldComponent_MilkCumAbsorptionDelay.ApplyProlactinMoodEffects(pawn, rawSeverity);
            return;
        }

        pawn.health.RemoveHediff(hediff);
        var world = Find.World;
        var delayComp = world?.GetComponent<WorldComponent_MilkCumAbsorptionDelay>();
        if (delayComp != null)
        {
            int endTick = Find.TickManager.TicksGame + WorldComponent_MilkCumAbsorptionDelay.GetAbsorptionDelayTicks(pawn);
            delayComp.ScheduleLactating(pawn, effectiveSeverity, endTick);
        }
        else
        {
            float deltaS = effectiveSeverity * MilkCumSettings.GetRaceDrugDeltaSMultiplier(pawn);
            MilkCumSettings.LactationLog($"Prolactin immediate apply: {pawn?.Name}, deltaS={deltaS:F3}");
            var reapply = pawn.health.GetOrAddHediff(HediffDefOf.Lactating, pawn.GetBreastOrChestPart()) as HediffWithComps;
            if (reapply?.comps != null)
                foreach (var c in reapply.comps)
                    if (c is HediffComp_EqualMilkingLactating comp) { comp.AddFromDrug(deltaS); break; }
            WorldComponent_MilkCumAbsorptionDelay.ApplyProlactinMoodEffects(pawn, rawSeverity);
            WorldComponent_MilkCumAbsorptionDelay.TryRecordProlactinIngestion(pawn);
        }
    }
}

/// <summary>水池模型：分娩时对 Lactating 应用剩余天数+=10、当前泌乳量+=基础值。</summary>
internal static class PoolModelBirthHelper
{
    public static void ApplyBirthPoolValues(Pawn mother)
    {
        var hediff = mother?.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating) as HediffWithComps;
        if (hediff?.comps == null) return;
        foreach (var c in hediff.comps)
        {
            if (c is HediffComp_EqualMilkingLactating comp)
            {
                comp.AddFromBirth();
                break;
            }
        }
    }
}
