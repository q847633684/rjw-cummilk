using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using rjw;
using MilkCum.Core;
using MilkCum.Milk.Helpers;

namespace MilkCum.RJW;

/// <summary>泌乳期时临时增大 RJW 胸部 Severity，离开泌乳期后恢复。仅修改本 mod 维护的“基础值”缓存并写回 Hediff.Severity。</summary>
public class RJWLactatingBreastSizeGameComponent : GameComponent
{
    private const float LactatingSeverityBonus = 0.15f;
    private const int TickInterval = 250;

    /// <summary>已施加增益的胸部 Hediff -> 施加前记录的 base severity。</summary>
    private static readonly Dictionary<Hediff, float> BreastBaseSeverity = new();

    public RJWLactatingBreastSizeGameComponent(Game game) : base(game) { }

    public override void FinalizeInit()
    {
        BreastBaseSeverity.Clear();
    }

    public override void GameComponentTick()
    {
        if (!EqualMilkingSettings.rjwBreastSizeEnabled || Find.TickManager.TicksGame % TickInterval != 0) return;
        foreach (Map map in Find.Maps)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null) continue;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn?.health?.hediffSet == null) continue;
                if (!pawn.IsInLactatingState() && !BreastBaseSeverity.Keys.Any(h => h?.pawn == pawn)) continue;
                ApplyOrRestoreBreastSeverity(pawn);
            }
        }
        CleanupDeadPawns();
    }

    private static void ApplyOrRestoreBreastSeverity(Pawn pawn)
    {
        bool inLactating = pawn.IsInLactatingState();
        IEnumerable<ISexPartHediff> breasts = pawn.GetBreasts();
        if (breasts == null) return;
        foreach (ISexPartHediff part in breasts)
        {
            Hediff hediff = GetAsHediff(part);
            if (hediff == null) continue;
            if (inLactating)
            {
                if (!BreastBaseSeverity.ContainsKey(hediff))
                {
                    BreastBaseSeverity[hediff] = hediff.Severity;
                    hediff.Severity = Mathf.Min(1f, hediff.Severity + LactatingSeverityBonus);
                }
            }
            else
            {
                if (BreastBaseSeverity.TryGetValue(hediff, out float baseSev))
                {
                    hediff.Severity = baseSev;
                    BreastBaseSeverity.Remove(hediff);
                }
            }
        }
    }

    private static Hediff GetAsHediff(ISexPartHediff part)
    {
        if (part == null) return null;
        if (part is Hediff h) return h;
        var prop = AccessTools.Property(part.GetType(), "AsHediff");
        return prop?.GetValue(part) as Hediff;
    }

    private static void CleanupDeadPawns()
    {
        List<Hediff> toRemove = null;
        foreach (Hediff h in BreastBaseSeverity.Keys)
        {
            if (h?.pawn == null || !h.pawn.Spawned)
            {
                toRemove ??= new List<Hediff>();
                toRemove.Add(h);
            }
        }
        if (toRemove != null)
        {
            foreach (Hediff h in toRemove)
                BreastBaseSeverity.Remove(h);
        }
    }
}

[HarmonyPatch(typeof(Game))]
public static class Game_FinalizeInit_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch("FinalizeInit")]
    static void Postfix(Game __instance)
    {
        if (__instance?.components == null) return;
        if (__instance.components.Any(c => c is RJWLactatingBreastSizeGameComponent)) return;
        __instance.components.Add(new RJWLactatingBreastSizeGameComponent(__instance));
    }
}
