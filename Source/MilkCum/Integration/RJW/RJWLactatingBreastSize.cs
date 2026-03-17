using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using rjw;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Helpers;

namespace MilkCum.RJW;

/// <summary>读档后待恢复项：用 Pawn LoadID + 乳房在 GetBreastList 中的序号定位 Hediff，恢复时填入 BreastBaseSeverity 避免重复施加。</summary>
public class SavedBreastBaseEntry : IExposable
{
    public string PawnLoadId;
    public int BreastIndex;
    public float BaseSeverity;

    public void ExposeData()
    {
        Scribe_Values.Look(ref PawnLoadId, "pawnLoadId");
        Scribe_Values.Look(ref BreastIndex, "breastIndex", 0);
        Scribe_Values.Look(ref BaseSeverity, "baseSeverity", 0f);
    }
}

/// <summary>泌乳期时临时增大 RJW 乳房体型，离开泌乳期后恢复。通过 RJW 的 HediffComp_SexPart.SetSeverity/GetSeverity 修改，同步更新 baseSize 与 parent.Severity，避免仅改 Hediff.Severity 被 SyncSeverity 覆盖。存读档时通过 PendingRestore 持久化「施加前 Severity」，读档后在 Tick 中解析回字典，避免重复施加。</summary>
public class RJWLactatingBreastSizeGameComponent : Verse.GameComponent
{
    private const float LactatingSeverityBonus = 0.15f;
    private const int TickInterval = 250;

    /// <summary>已施加增益的胸部 Hediff -> 施加前记录的原始 Severity（来自 RJW Comp.GetSeverity()），恢复时用 SetSeverity 写回。</summary>
    private static readonly Dictionary<Hediff, float> BreastBaseSeverity = new();

    /// <summary>读档后待解析列表：存 (Pawn LoadID, 乳房序号, 原始 Severity)，在 Tick 中遇到对应 Pawn 时解析回 BreastBaseSeverity。</summary>
    private List<SavedBreastBaseEntry> _pendingRestore = new();

    /// <summary>与当前引用的 Assembly-CSharp 中 GameComponent 无参构造兼容；game 由框架在加入 Game.components 时关联。</summary>
    public RJWLactatingBreastSizeGameComponent(Verse.Game game)
        : base() { }

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            _pendingRestore.Clear();
            foreach (var kv in BreastBaseSeverity.ToList())
            {
                if (kv.Key?.pawn == null) continue;
                var list = kv.Key.pawn.GetBreastList();
                if (list == null) continue;
                int idx = list.IndexOf(kv.Key);
                if (idx < 0) continue;
                string loadId = kv.Key.pawn.GetUniqueLoadID();
                if (string.IsNullOrEmpty(loadId)) continue;
                _pendingRestore.Add(new SavedBreastBaseEntry { PawnLoadId = loadId, BreastIndex = idx, BaseSeverity = kv.Value });
            }
        }
        Scribe_Collections.Look(ref _pendingRestore, "EM.BreastBaseSeverityPending", LookMode.Deep);
        if (_pendingRestore == null)
            _pendingRestore = new List<SavedBreastBaseEntry>();
    }

    public override void FinalizeInit()
    {
        BreastBaseSeverity.Clear();
    }

    public override void GameComponentTick()
    {
        if (!MilkCumSettings.rjwBreastSizeEnabled || Find.TickManager.TicksGame % TickInterval != 0) return;
        foreach (Map map in Find.Maps)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null) continue;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn?.health?.hediffSet == null) continue;
                ResolvePendingForPawn(this, pawn);
                if (!pawn.IsInLactatingState() && !BreastBaseSeverity.Keys.Any(h => h?.pawn == pawn)) continue;
                ApplyOrRestoreBreastSeverity(pawn);
                if (pawn.IsInLactatingState() && pawn.LactatingHediffComp() is { } lactatingComp && lactatingComp.TryConsumeNextPermanentGainMilestone())
                    ApplyPermanentBreastGain(pawn);
            }
        }
        CleanupDeadPawns();
    }

    /// <summary>对当前泌乳 pawn 的每个 RJW 乳房：用 BreastBaseSeverity 中的 base 加上 rjwPermanentBreastGainSeverityDelta 作为新 base，写回字典并 SetSeverity(newBase + 临时增益)，不在本 mod 维护单独体型数字。</summary>
    private static void ApplyPermanentBreastGain(Pawn pawn)
    {
        if (!MilkCumSettings.rjwBreastSizeEnabled || !MilkCumSettings.rjwPermanentBreastGainFromLactationEnabled) return;
        float delta = Mathf.Max(0f, MilkCumSettings.rjwPermanentBreastGainSeverityDelta);
        if (delta <= 0f) return;
        IEnumerable<ISexPartHediff> breasts = pawn.GetBreasts();
        if (breasts == null) return;
        foreach (ISexPartHediff part in breasts)
        {
            Hediff hediff = GetAsHediff(part);
            if (hediff == null) continue;
            HediffComp_SexPart comp = part.GetPartComp();
            if (comp == null) continue;
            if (!BreastBaseSeverity.TryGetValue(hediff, out float baseSev)) continue;
            float newBase = Mathf.Min(1f, baseSev + delta);
            BreastBaseSeverity[hediff] = newBase;
            comp.SetSeverity(Mathf.Min(1f, newBase + LactatingSeverityBonus));
        }
    }

    /// <summary>读档后：若该 Pawn 有待恢复项，按 LoadID+序号找到 Hediff 并填入 BreastBaseSeverity，避免重复施加。</summary>
    private static void ResolvePendingForPawn(RJWLactatingBreastSizeGameComponent self, Pawn pawn)
    {
        if (self._pendingRestore == null || self._pendingRestore.Count == 0) return;
        string loadId = pawn.GetUniqueLoadID();
        if (string.IsNullOrEmpty(loadId)) return;
        var list = pawn.GetBreastList();
        if (list == null) return;
        for (int i = self._pendingRestore.Count - 1; i >= 0; i--)
        {
            var e = self._pendingRestore[i];
            if (e.PawnLoadId != loadId) continue;
            if (e.BreastIndex < 0 || e.BreastIndex >= list.Count) { self._pendingRestore.RemoveAt(i); continue; }
            Hediff h = list[e.BreastIndex];
            if (h != null && !BreastBaseSeverity.ContainsKey(h))
                BreastBaseSeverity[h] = e.BaseSeverity;
            self._pendingRestore.RemoveAt(i);
        }
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
            HediffComp_SexPart comp = part.GetPartComp();
            if (comp == null) continue;
            if (inLactating)
            {
                if (!BreastBaseSeverity.ContainsKey(hediff))
                {
                    float origSev = comp.GetSeverity();
                    BreastBaseSeverity[hediff] = origSev;
                    comp.SetSeverity(UnityEngine.Mathf.Min(1f, origSev + LactatingSeverityBonus));
                }
            }
            else
            {
                if (BreastBaseSeverity.TryGetValue(hediff, out float baseSev))
                {
                    comp.SetSeverity(baseSev);
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
