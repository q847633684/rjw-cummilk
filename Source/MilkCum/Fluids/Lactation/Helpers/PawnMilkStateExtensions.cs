using System.Collections.Generic;
using MilkCum.Core;
using RimWorld;
using Verse;

using static MilkCum.Fluids.Lactation.Helpers.Categories;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 泌乳状态与分类：是否可产奶、是否泌乳、泌乳状态缓存、默认奶设置。由 ExtensionHelper 拆出，见 记忆库/design/架构原则与重组建议。
/// </summary>
public static class PawnMilkStateExtensions
{
    private const int LactatingStateCacheTicks = 60;
    private static readonly Dictionary<Pawn, (bool value, int tick)> LactatingStateCache = new();

    public static MilkSettings GetDefaultMilkSetting(this Pawn pawn)
    {
        if (pawn == null) { return null; }
        return GetPawnCategory(pawn) switch
        {
            PawnCategory.Colonist => MilkCumSettings.colonistSetting.Copy(),
            PawnCategory.Prisoner => MilkCumSettings.prisonerSetting.Copy(),
            PawnCategory.Slave => MilkCumSettings.slaveSetting.Copy(),
            PawnCategory.Animal => MilkCumSettings.animalSetting.Copy(),
            PawnCategory.Mechanoid => MilkCumSettings.mechSetting.Copy(),
            PawnCategory.Entity => MilkCumSettings.entitySetting.Copy(),
            _ => null,
        };
    }

    public static bool IsMilkable(this Pawn pawn) => MilkCumSettings.IsMilkable(pawn);
    public static bool IsLactating(this Pawn pawn) => pawn?.health?.hediffSet?.HasHediff(HediffDefOf.Lactating) ?? false;

    /// <summary>是否有药物诱发的泌乳（催乳素耐受或成瘾），用于 UI 显示与添加「药物泌乳负担」等。</summary>
    public static bool HasDrugInducedLactation(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null) return false;
        return (MilkCumDefOf.EM_Prolactin_Tolerance != null && pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_Prolactin_Tolerance) != null)
            || (MilkCumDefOf.EM_Prolactin_Addiction != null && pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_Prolactin_Addiction) != null);
    }

    /// <summary>统一泌乳判断：本 mod Lactating、或 RJW 的 Lactating_Drug / Lactating_Permanent / Heavy_Lactating_Permanent。带 60 tick 缓存。</summary>
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
