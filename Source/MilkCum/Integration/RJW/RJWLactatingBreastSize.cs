using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using rjw;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Data;

namespace MilkCum.RJW;

/// <summary>泌乳期临时增大 RJW 乳房 Severity，离乳后恢复。基准 Severity 存静态字典；随游戏存档为键值对列表（<see cref="BreastBaseSeveritySavePair"/>），读入时一次性灌回字典。</summary>
public class RJWLactatingBreastSizeGameComponent : Verse.GameComponent
{
    private const int TickInterval = 250;
    private const int AllowedListsCleanupInterval = 2500;

    /// <summary>key = "{Pawn.GetUniqueLoadID()}:{breastListIndex}" → 施加临时胀大前的基准 Severity。</summary>
    private static readonly Dictionary<string, float> BreastBaseSeverity = new();

    private sealed class BreastBaseSeveritySavePair : IExposable
    {
        public string Key;
        public float Value;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Key, "k");
            Scribe_Values.Look(ref Value, "v", 0f);
        }
    }

    private List<BreastBaseSeveritySavePair> _breastBaseSaveScratch = new();

    public RJWLactatingBreastSizeGameComponent(Verse.Game game)
        : base() { }

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            _breastBaseSaveScratch = new List<BreastBaseSeveritySavePair>();
            foreach (var kv in BreastBaseSeverity)
                _breastBaseSaveScratch.Add(new BreastBaseSeveritySavePair { Key = kv.Key, Value = kv.Value });
        }
        Scribe_Collections.Look(ref _breastBaseSaveScratch, "EM.RjwBreastBaseSeverity", LookMode.Deep);
        if (_breastBaseSaveScratch == null)
            _breastBaseSaveScratch = new List<BreastBaseSeveritySavePair>();
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            BreastBaseSeverity.Clear();
            foreach (var e in _breastBaseSaveScratch)
            {
                if (e != null && !string.IsNullOrEmpty(e.Key))
                    BreastBaseSeverity[e.Key] = e.Value;
            }
        }
    }

    private static string KeyFor(Pawn pawn, int breastIndex)
    {
        if (pawn == null || breastIndex < 0) return null;
        string loadId = pawn.GetUniqueLoadID();
        return string.IsNullOrEmpty(loadId) ? null : $"{loadId}:{breastIndex}";
    }

    private static (string loadId, int index) ParseKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return (null, -1);
        int colon = key.IndexOf(':');
        if (colon < 0) return (null, -1);
        if (!int.TryParse(key.Substring(colon + 1), out int index)) return (null, -1);
        return (key.Substring(0, colon), index);
    }

    public override void GameComponentTick()
    {
        int ticks = Find.TickManager.TicksGame;
        if (ticks % AllowedListsCleanupInterval == 0)
        {
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns?.AllPawnsSpawned == null) continue;
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    var comp = pawn?.CompEquallyMilkable();
                    if (comp != null)
                        comp.CleanupAllowedLists();
                }
            }
        }
        if (!ModIntegrationGates.RjwModActive || ticks % TickInterval != 0) return;
        foreach (Map map in Find.Maps)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null) continue;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn?.health?.hediffSet == null) continue;
                if (pawn.IsInLactatingState())
                {
                    if (pawn.LactatingHediffComp() is { } lactatingComp && lactatingComp.TryConsumeNextPermanentGainMilestone())
                        ApplyPermanentBreastGain(pawn);
                }
                else
                {
                    if (!HasAnyKeyForPawn(pawn)) continue;
                    ApplyOrRestoreBreastSeverity(pawn);
                }
            }
        }
        if ((Find.TickManager.TicksGame / TickInterval) % 10 == 0)
            CleanupDeadPawns();
    }

    private static float BreastPartSeverityCap(Hediff hediff)
    {
        if (hediff?.def == null) return 1f;
        float m = hediff.def.maxSeverity;
        return m > 0.001f ? m : 1f;
    }

    private static bool HasAnyKeyForPawn(Pawn pawn)
    {
        if (pawn == null) return false;
        string loadId = pawn.GetUniqueLoadID();
        if (string.IsNullOrEmpty(loadId)) return false;
        string prefix = loadId + ":";
        return BreastBaseSeverity.Keys.Any(k => k != null && k.StartsWith(prefix));
    }

    private static void ApplyPermanentBreastGain(Pawn pawn)
    {
        if (!ModIntegrationGates.RjwModActive || !MilkCumSettings.rjwPermanentBreastGainFromLactationEnabled) return;
        float delta = Mathf.Max(0f, MilkCumSettings.rjwPermanentBreastGainSeverityDelta);
        if (delta <= 0f) return;
        foreach (var s in RjwBreastPoolEconomy.GetBreastPoolSnapshots(pawn))
        {
            string key = KeyFor(pawn, s.ListIndex);
            if (key == null || !BreastBaseSeverity.TryGetValue(key, out float baseSev)) continue;
            Hediff h = s.BreastHediff;
            if (h == null) continue;
            float cap = BreastPartSeverityCap(h);
            float newBase = Mathf.Min(cap, baseSev + delta);
            BreastBaseSeverity[key] = newBase;
        }
        SyncRJWBreastSeverityFromPool(pawn);
    }

    public static void SyncRJWBreastSeverityFromPool(Pawn pawn)
    {
        if (pawn == null || !ModIntegrationGates.RjwModActive || !pawn.IsInLactatingState()) return;
        var milkComp = pawn.CompEquallyMilkable();
        if (milkComp == null) return;
        float baseTotal = milkComp.GetPoolBaseTotal();
        float stretchTotal = milkComp.GetPoolStretchTotal();
        float fullness = milkComp.Fullness;
        float t_L = 0f;
        if (pawn.LactatingHediffComp() is { } lactatingComp)
        {
            float L = lactatingComp.EffectiveLactationAmountForFlow;
            float cap = MilkCumSettings.lactationLevelCap;
            t_L = cap > 0f ? Mathf.Clamp01(L / cap) : Mathf.Clamp01(L);
        }
        float t_pool = 0f;
        if (stretchTotal > baseTotal && fullness > baseTotal)
            t_pool = Mathf.Clamp01((fullness - baseTotal) / (stretchTotal - baseTotal));
        float t_poolStretchAggregate = MilkRealismHelper.QuantizePoolStretchT(t_pool);
        bool perSideStretch = MilkCumSettings.realismRjwStretchPerSideSync;
        List<FluidPoolEntry> entries = null;
        if (perSideStretch)
            entries = milkComp.GetCachedEntriesIfValid() ?? pawn.GetBreastPoolEntries();
        bool anySeverityMoved = false;
        var snaps = RjwBreastPoolEconomy.GetBreastPoolSnapshots(pawn);
        for (int idx = 0; idx < snaps.Count; idx++)
        {
            var s = snaps[idx];
            int i = s.ListIndex;
            Hediff hediff = s.BreastHediff;
            if (hediff == null) continue;
            HediffComp_SexPart comp = (hediff as ISexPartHediff)?.GetPartComp();
            if (comp == null) continue;
            string key = KeyFor(pawn, i);
            if (key == null) continue;
            if (!BreastBaseSeverity.ContainsKey(key))
            {
                float origSev = comp.GetSeverity();
                BreastBaseSeverity[key] = origSev;
            }
            if (BreastBaseSeverity.TryGetValue(key, out float baseSev))
            {
                float tStretch = t_poolStretchAggregate;
                if (perSideStretch && entries != null && !string.IsNullOrEmpty(s.PoolKey))
                {
                    float baseSide = RjwBreastPoolEconomy.CapacitySumForStableBreastBaseKey(entries, s.PoolKey);
                    if (baseSide < PoolModelConstants.Epsilon)
                        baseSide = Mathf.Max(PoolModelConstants.Epsilon, s.BaseCapacityPerSide * 2f);
                    float vanillaStretch = baseSide * PoolModelConstants.StretchCapFactor;
                    float stretchSide = MilkRealismHelper.GetPerSideStretchCapFromBase(baseSide, vanillaStretch);
                    string b = s.PoolKey;
                    float fulSide = milkComp.GetFullnessForKey(RjwBreastPoolEconomy.AppendVirtualBreastStorageSuffix(b, true))
                        + milkComp.GetFullnessForKey(RjwBreastPoolEconomy.AppendVirtualBreastStorageSuffix(b, false));
                    float tSide = 0f;
                    if (stretchSide > baseSide && fulSide > baseSide)
                        tSide = Mathf.Clamp01((fulSide - baseSide) / (stretchSide - baseSide));
                    tStretch = MilkRealismHelper.QuantizePoolStretchT(tSide);
                }
                float target = baseSev + MilkCumSettings.rjwLactatingSeverityBonus * t_L
                    + MilkCumSettings.rjwLactatingStretchSeverityBonus * tStretch;
                float capSev = BreastPartSeverityCap(hediff);
                float clamped = Mathf.Min(capSev, target);
                float prev = comp.GetSeverity();
                comp.SetSeverity(clamped);
                if (Mathf.Abs(prev - clamped) > 1e-5f)
                    anySeverityMoved = true;
            }
        }
        if (anySeverityMoved)
            milkComp.SetEntriesCacheDirty();
    }

    private static void ApplyOrRestoreBreastSeverity(Pawn pawn)
    {
        foreach (var s in RjwBreastPoolEconomy.GetBreastPoolSnapshots(pawn))
        {
            string key = KeyFor(pawn, s.ListIndex);
            if (key == null || !BreastBaseSeverity.TryGetValue(key, out float baseSev)) continue;
            Hediff hediff = s.BreastHediff;
            if (hediff == null) continue;
            HediffComp_SexPart comp = (hediff as ISexPartHediff)?.GetPartComp();
            if (comp != null)
                comp.SetSeverity(baseSev);
            BreastBaseSeverity.Remove(key);
        }
    }

    private static void CleanupDeadPawns()
    {
        var validLoadIds = new HashSet<string>();
        foreach (Map map in Find.Maps)
        {
            if (map?.mapPawns?.AllPawns == null) continue;
            foreach (Pawn p in map.mapPawns.AllPawns)
            {
                string id = p?.GetUniqueLoadID();
                if (!string.IsNullOrEmpty(id)) validLoadIds.Add(id);
            }
        }
        if (Find.WorldPawns != null)
        {
            foreach (Pawn p in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                string id = p?.GetUniqueLoadID();
                if (!string.IsNullOrEmpty(id)) validLoadIds.Add(id);
            }
        }
        List<string> toRemove = null;
        foreach (string key in BreastBaseSeverity.Keys)
        {
            var (loadId, _) = ParseKey(key);
            if (string.IsNullOrEmpty(loadId) || !validLoadIds.Contains(loadId))
            {
                toRemove ??= new List<string>();
                toRemove.Add(key);
            }
        }
        if (toRemove != null)
        {
            foreach (string k in toRemove)
                BreastBaseSeverity.Remove(k);
        }
    }
}

[HarmonyPatch(typeof(Verse.Game))]
public static class Game_FinalizeInit_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch("FinalizeInit")]
    static void Postfix(Verse.Game __instance)
    {
        if (__instance?.components == null) return;
        if (__instance.components.Any(c => c is RJWLactatingBreastSizeGameComponent)) return;
        __instance.components.Add(new RJWLactatingBreastSizeGameComponent(__instance));
    }
}
