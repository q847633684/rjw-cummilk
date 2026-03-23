using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using rjw;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
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
    private const int TickInterval = 250;
    private const int AllowedListsCleanupInterval = 2500;

    /// <summary>稳定 key = "{Pawn.GetUniqueLoadID()}:{breastIndex}" -> 施加前记录的原始 Severity；避免 Hediff 实例被替换后 key 失效导致泄漏与恢复失败。</summary>
    private static readonly Dictionary<string, float> BreastBaseSeverity = new();

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
                var (loadId, index) = ParseKey(kv.Key);
                if (string.IsNullOrEmpty(loadId) || index < 0) continue;
                _pendingRestore.Add(new SavedBreastBaseEntry { PawnLoadId = loadId, BreastIndex = index, BaseSeverity = kv.Value });
            }
        }
        Scribe_Collections.Look(ref _pendingRestore, "EM.BreastBaseSeverityPending", LookMode.Deep);
        if (_pendingRestore == null)
            _pendingRestore = new List<SavedBreastBaseEntry>();
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

    /// <summary>GameComponent 与 Game 同生命周期，新档会 new 新实例，无需清空字典。</summary>
    public override void FinalizeInit() { }

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
        if (!MilkCumSettings.rjwBreastSizeEnabled || ticks % TickInterval != 0) return;
        foreach (Map map in Find.Maps)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null) continue;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn?.health?.hediffSet == null) continue;
                ResolvePendingForPawn(this, pawn);
                if (pawn.IsInLactatingState())
                {
                    // 泌乳中：Sync 由 CompEquallyMilkable.CompTick 每 60 tick 在 UpdateMilkPools 后驱动，此处仅处理永久撑大里程碑
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

    /// <summary>RJW 乳房 Hediff 的合法 Severity 上限：优先用 def.maxSeverity，避免硬编码 1f 截断泌乳/撑大增益。</summary>
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

    /// <summary>对当前泌乳 pawn 的每个 RJW 乳房：用 BreastBaseSeverity 中的 base 加上 rjwPermanentBreastGainSeverityDelta 作为新 base，写回字典并 SetSeverity(newBase + 临时增益)，不在本 mod 维护单独体型数字。</summary>
    private static void ApplyPermanentBreastGain(Pawn pawn)
    {
        if (!MilkCumSettings.rjwBreastSizeEnabled || !MilkCumSettings.rjwPermanentBreastGainFromLactationEnabled) return;
        float delta = Mathf.Max(0f, MilkCumSettings.rjwPermanentBreastGainSeverityDelta);
        if (delta <= 0f) return;
        var list = pawn.GetBreastListOrEmpty();
        for (int i = 0; i < list.Count; i++)
        {
            string key = KeyFor(pawn, i);
            if (key == null || !BreastBaseSeverity.TryGetValue(key, out float baseSev)) continue;
            Hediff h = list[i];
            float cap = BreastPartSeverityCap(h);
            float newBase = Mathf.Min(cap, baseSev + delta);
            BreastBaseSeverity[key] = newBase;
        }
        SyncRJWBreastSeverityFromPool(pawn);
    }

    /// <summary>读档后：若该 Pawn 有待恢复项，按 LoadID+序号找到 Hediff 并填入 BreastBaseSeverity，避免重复施加。</summary>
    private static void ResolvePendingForPawn(RJWLactatingBreastSizeGameComponent self, Pawn pawn)
    {
        if (self._pendingRestore == null || self._pendingRestore.Count == 0) return;
        string loadId = pawn.GetUniqueLoadID();
        if (string.IsNullOrEmpty(loadId)) return;
        var list = pawn.GetBreastListOrEmpty();
        for (int i = self._pendingRestore.Count - 1; i >= 0; i--)
        {
            var e = self._pendingRestore[i];
            if (e.PawnLoadId != loadId) continue;
            if (e.BreastIndex < 0 || e.BreastIndex >= list.Count) { self._pendingRestore.RemoveAt(i); continue; }
            string key = KeyFor(pawn, e.BreastIndex);
            if (key != null && !BreastBaseSeverity.ContainsKey(key))
                BreastBaseSeverity[key] = e.BaseSeverity;
            self._pendingRestore.RemoveAt(i);
        }
    }

    /// <summary>根据当前奶池水位与 L 同步 RJW 乳房 Severity：0~1 基础由 L 驱动，1~1.2 撑大由池 Fullness 驱动；回缩时每 60 tick 池更新后调用本方法即同步减少 RJW。由 ApplyOrRestoreBreastSeverity 与 CompEquallyMilkable.CompTick（UpdateMilkPools 后）调用。</summary>
    public static void SyncRJWBreastSeverityFromPool(Pawn pawn)
    {
        if (pawn == null || !MilkCumSettings.rjwBreastSizeEnabled || !pawn.IsInLactatingState()) return;
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
        var list = pawn.GetBreastListOrEmpty();
        for (int i = 0; i < list.Count; i++)
        {
            Hediff hediff = list[i];
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
                float target = baseSev + MilkCumSettings.rjwLactatingSeverityBonus * t_L + MilkCumSettings.rjwLactatingStretchSeverityBonus * t_pool;
                comp.SetSeverity(Mathf.Min(BreastPartSeverityCap(hediff), target));
            }
        }
    }

    /// <summary>仅用于非泌乳 pawn：恢复 RJW 乳房 Severity 到记录的基础值并移出字典。泌乳中的 Sync 由 CompEquallyMilkable.CompTick 每 60 tick 驱动。</summary>
    private static void ApplyOrRestoreBreastSeverity(Pawn pawn)
    {
        var list = pawn.GetBreastListOrEmpty();
        for (int i = 0; i < list.Count; i++)
        {
            string key = KeyFor(pawn, i);
            if (key == null) continue;
            if (!BreastBaseSeverity.TryGetValue(key, out float baseSev)) continue;
            Hediff hediff = list[i];
            if (hediff == null) continue;
            HediffComp_SexPart comp = (hediff as ISexPartHediff)?.GetPartComp();
            if (comp != null)
                comp.SetSeverity(baseSev);
            BreastBaseSeverity.Remove(key);
        }
    }

    private static Hediff GetAsHediff(ISexPartHediff part)
    {
        if (part == null) return null;
        if (part is Hediff h) return h;
        var prop = AccessTools.Property(part.GetType(), "AsHediff");
        return prop?.GetValue(part) as Hediff;
    }

    /// <summary>清理已不存在于任何地图的 pawn 的 BreastBaseSeverity 条目；用 AllPawns（含尸体/未生成）构建有效 loadID 集合，防止读档丢失。若 RimWorld 提供 AllPawnsEverAlive 可改用。</summary>
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
