using System.Collections.Generic;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>开发模式：乳池条目异常提示（限频），便于 HAR/异种体侧向未标注等排查。</summary>
public static class BreastPoolTopologyDiagnostics
{
    private const int CooldownTicks = 2500;
    private static readonly Dictionary<string, int> LastWarnTick = new();

    private static bool TryConsumeWarnSlot(string key, int now)
    {
        if (LastWarnTick.TryGetValue(key, out int t) && now - t < CooldownTicks)
        {
            return false;
        }

        LastWarnTick[key] = now;
        return true;
    }

    /// <summary>
    /// 在得到当日 <see cref="FluidPoolEntry"/> 列表后调用。仅 DevMode。
    /// </summary>
    public static void MaybeDevWarnAfterEntriesBuilt(Pawn pawn, List<FluidPoolEntry> entries)
    {
        if (!Prefs.DevMode || pawn == null || entries == null || entries.Count == 0)
        {
            return;
        }

        int now = Find.TickManager?.TicksGame ?? 0;
        string id = pawn.ThingID.ToString();

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Site != FluidSiteKind.None)
            {
                continue;
            }

            string k = $"siteNone:{id}";
            if (!TryConsumeWarnSlot(k, now))
            {
                continue;
            }

            Log.Warning($"[MilkCum][Dev] Pawn {pawn.LabelShort}: breast pool entry #{i} has FluidSiteKind.None (unlabeled lateral part?). Key={entries[i].Key}");
        }
    }

    /// <summary><see cref="PawnMilkPoolExtensions.GetPoolKeyForBreastHediff"/> 未匹配侧行时（乳房 Hediff 在池构象中缺席）。</summary>
    public static void MaybeDevLogPoolKeyLookupMiss(Pawn pawn, Hediff breastHediff)
    {
        if (!Prefs.DevMode || pawn == null || breastHediff == null)
        {
            return;
        }

        if (!RjwBreastPoolEconomy.IsBreastHediffForPool(breastHediff))
        {
            return;
        }

        int now = Find.TickManager?.TicksGame ?? 0;
        string k = $"pkMiss:{pawn.ThingID}:{breastHediff.def?.defName ?? "?"}";
        if (!TryConsumeWarnSlot(k, now))
        {
            return;
        }

        Log.Warning($"[MilkCum][Dev] GetPoolKeyForBreastHediff: no side row for breast {breastHediff.def?.defName} on {pawn.LabelShort} (Part={breastHediff.Part?.def?.defName}). Caller may get null.");
    }
}
