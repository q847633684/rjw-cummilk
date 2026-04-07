using System;
using HarmonyLib;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Helpers;
using RimWorld;
using Verse;

namespace MilkCum.Harmony;

[HarmonyPatch(typeof(Hediff_Pregnant))]
public static class Hediff_Pregnant_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Hediff_Pregnant.DoBirthSpawn))]
    public static void DoBirthSpawn_Prefix(Pawn mother)
    {
        if (mother.IsNormalAnimal())
        {
            mother.health.GetOrAddHediff(HediffDefOf.Lactating, mother.GetBreastOrChestPart());
            PoolModelBirthHelper.ApplyBirthPoolValues(mother);
            MilkCumSettings.LactationLog($"Birth lactation (animal): {mother.Name}");
            return;
        }
        if (mother.RaceProps?.Humanlike == true && mother.health?.hediffSet != null
            && !mother.health.hediffSet.HasHediff(HediffDefOf.Lactating))
        {
            mother.health.GetOrAddHediff(HediffDefOf.Lactating, mother.GetBreastOrChestPart());
        }
        if (mother.RaceProps?.Humanlike == true)
        {
            PoolModelBirthHelper.ApplyBirthPoolValues(mother);
            MilkCumSettings.LactationLog($"Birth lactation (humanlike): {mother.Name}");
        }
        MilkPermissionExtensions.TryGiveFirstLactationBirthMemory(mother);
    }
}

[HarmonyPatch(typeof(Need_Food))]
public static class Need_Food_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Need_Food.GetTipString))]
    public static void GetTipString_PostFix(Need_Food __instance, ref string __result)
    {
        try
        {
            if (__instance?.pawn is not Pawn pawn || !pawn.IsMilkable()) return;
            if (pawn.health?.hediffSet == null || pawn.needs?.food == null) return;
            string hungerLabel = Lang.HungerRate;
            if (!string.IsNullOrEmpty(__result) && !string.IsNullOrEmpty(hungerLabel) && __result.Contains(hungerLabel))
                return;
            __result += "\n" + hungerLabel + ": ";
            HungerCategory curCategory = pawn.needs.food.CurCategory;
            if (curCategory != HungerCategory.Fed)
            {
                __result += "\n   -" + curCategory.Label() + ": x" + HungerLevelUtility.HungerMultiplier(curCategory).ToStringPercent();
            }
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                HediffStage curStage = hediff?.CurStage;
                if (curStage == null) { continue; }
                if (curStage.hungerRateFactor != 1f)
                {
                    __result += "\n   -" + hediff.LabelCap + ": x" + hediff.CurStage.hungerRateFactor.ToStringPercent();
                }
                if (curStage.hungerRateFactorOffset != 0f)
                {
                    if (curStage.hungerRateFactorOffset > 0f)
                    {
                        __result += "\n   -" + hediff.LabelCap + ": +" + hediff.CurStage.hungerRateFactorOffset.ToStringByStyle(ToStringStyle.PercentZero);
                    }
                    else
                    {
                        __result += "\n   -" + hediff.LabelCap + ": " + hediff.CurStage.hungerRateFactorOffset.ToStringByStyle(ToStringStyle.PercentZero);
                    }
                }
            }
            __result += "\n" + "StatsReport_FinalValue".Translate() + " " + (pawn.health.hediffSet.GetHungerRateFactor() * HungerLevelUtility.HungerMultiplier(curCategory)).ToStringPercent();
        }
        catch (Exception ex)
        {
            Log.Warning($"[MilkCum] Need_Food.GetTipString postfix: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(Pawn_HealthTracker))]
[HarmonyPatch(nameof(Pawn_HealthTracker.RemoveHediff))]
public static class HediffSet_Remove_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn_HealthTracker __instance, Hediff hediff)
    {
        if (hediff?.def != HediffDefOf.Lactating) return;
        Pawn pawn = hediff.pawn;
        if (pawn == null) return;
        MilkCumSettings.LactationLog($"Lactating removed, ClearPools: {pawn.Name}");
        var comp = pawn.CompEquallyMilkable();
        if (comp != null)
            comp.ClearPools();
        PawnMilkStateExtensions.ClearLactatingStateCache(pawn);
    }
}
