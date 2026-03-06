using System.Collections.Generic;
using MilkCum.Core;
using RimWorld;
using Verse;

using static MilkCum.Fluids.Lactation.Helpers.Categories;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 泌乳状态与分类：是否可产奶、是否泌乳、泌乳状态缓存、默认奶设置、基础营养/天�?/// �?ExtensionHelper 拆出，见 记忆�?design/架构原则与重组建议�?/// </summary>
public static class PawnMilkStateExtensions
{
    private const int LactatingStateCacheTicks = 60;
    private static readonly Dictionary<Pawn, (bool value, int tick)> LactatingStateCache = new();

    public static MilkSettings GetDefaultMilkSetting(this Pawn pawn)
    {
        if (pawn == null) { return null; }
        return GetPawnCategory(pawn) switch
        {
            PawnCategory.Colonist => EqualMilkingSettings.colonistSetting.Copy(),
            PawnCategory.Prisoner => EqualMilkingSettings.prisonerSetting.Copy(),
            PawnCategory.Slave => EqualMilkingSettings.slaveSetting.Copy(),
            PawnCategory.Animal => EqualMilkingSettings.animalSetting.Copy(),
            PawnCategory.Mechanoid => EqualMilkingSettings.mechSetting.Copy(),
            PawnCategory.Entity => EqualMilkingSettings.entitySetting.Copy(),
            _ => null,
        };
    }

    public static float BaseNutritionPerDay(this Pawn p)
    {
        return p.ageTracker.CurLifeStage.hungerRateFactor * p.RaceProps.baseHungerRate * 2.6666667E-05f * 60000f;
    }

    public static bool IsMilkable(this Pawn pawn) => EqualMilkingSettings.IsMilkable(pawn);
    public static bool HasNutritiousMilk(this Pawn pawn) => pawn.IsMilkable() && (pawn.MilkDef().ingestible?.CachedNutrition ?? 0) > 0;
    public static bool HasDrugMilk(this Pawn pawn) => pawn.IsMilkable() && ((pawn.MilkDef().ingestible?.drugCategory ?? DrugCategory.None) != DrugCategory.None);
    public static bool HasEdibleMilk(this Pawn pawn) => pawn.HasNutritiousMilk() || pawn.HasDrugMilk();
    public static bool IsLactating(this Pawn pawn) => pawn?.health?.hediffSet?.HasHediff(HediffDefOf.Lactating) ?? false;

    /// <summary>是否有药物诱发的泌乳（催乳素耐受或成瘾），用于显�?添加「药物泌乳负担」等</summary>
    public static bool HasDrugInducedLactation(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null) return false;
        return (EMDefOf.EM_Prolactin_Tolerance != null && pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Tolerance) != null)
            || (EMDefOf.EM_Prolactin_Addiction != null && pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Addiction) != null);
    }

    /// <summary>统一泌乳判断：本�?Lactating �?RJW �?Lactating_Drug / Lactating_Permanent / Heavy_Lactating_Permanent。带 60 tick 缓存</summary>
    public static bool IsInLactatingState(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null) return false;
        int now = Find.TickManager.TicksGame;
        if (LactatingStateCache.TryGetValue(pawn, out var cached) && now - cached.tick < LactatingStateCacheTicks)
            return cached.value;
        bool value = pawn.health.hediffSet.HasHediff(HediffDefOf.Lactating)
            || (DefDatabase<HediffDef>.GetNamedSilentFail("Lactating_Drug") is HediffDef d1 && pawn.health.hediffSet.HasHediff(d1))
            || (DefDatabase<HediffDef>.GetNamedSilentFail("Lactating_Permanent") is HediffDef d2 && pawn.health.hediffSet.HasHediff(d2))
            || (DefDatabase<HediffDef>.GetNamedSilentFail("Heavy_Lactating_Permanent") is HediffDef d3 && pawn.health.hediffSet.HasHediff(d3));
        LactatingStateCache[pawn] = (value, now);
        return value;
    }

    /// <summary>清除泌乳缓存（如 hediff 变更时可调，非必须）</summary>
    public static void ClearLactatingStateCache(Pawn pawn = null)
    {
        if (pawn == null) LactatingStateCache.Clear();
        else LactatingStateCache.Remove(pawn);
    }
}
