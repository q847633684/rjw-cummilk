using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;

namespace MilkCum.Fluids.Lactation.World;

/// <summary>吸收延迟待生效条目：�?endTick 时给 pawn 添加 Lactating 并执�?AddFromDrug(deltaSeverity)。见 Docs/泌乳系统逻辑图</summary>
public class PendingLactatingEntry : IExposable
{
    public Pawn pawn;
    public float rawSeverity;
    public int endTick;
    /// <summary>吃药前的耐受严重�?t_before，用于计�?Δs �?E_tol</summary>
    public float toleranceBefore;

    public void ExposeData()
    {
        Scribe_References.Look(ref pawn, "pawn");
        Scribe_Values.Look(ref rawSeverity, "severity", 0f);
        Scribe_Values.Look(ref endTick, "endTick", 0);
        Scribe_Values.Look(ref toleranceBefore, "toleranceBefore", 0f);
    }
}

/// <summary>水池模型吸收延迟：吃药后延迟一段时间再�?Lactating 并进水，延迟由代谢率决定。见 Docs/泌乳系统逻辑图</summary>
public class WorldComponent_MilkCumAbsorptionDelay : WorldComponent
{
    private List<PendingLactatingEntry> pending = new List<PendingLactatingEntry>();

    public WorldComponent_MilkCumAbsorptionDelay(global::RimWorld.Planet.World world) : base(world) { }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref pending, "pending", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (pending == null) pending = new List<PendingLactatingEntry>();
            RemoveInvalidPendingEntries();
        }
    }

    /// <summary>加载后移�?pawn �?null 或已销毁的条目，避免跨存档/地图引用。见 Docs/泌乳系统逻辑图</summary>
    private void RemoveInvalidPendingEntries()
    {
        if (pending == null) return;
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            var e = pending[i];
            if (e.pawn == null || e.pawn.Destroyed || (e.pawn.Spawned == false && e.pawn.Dead))
                pending.RemoveAt(i);
        }
    }

    public void ScheduleLactating(Pawn p, float rawSeverity, int endTick, float toleranceBefore)
    {
        if (p == null || endTick <= 0) return;
        if (pending == null) pending = new List<PendingLactatingEntry>();
        int now = Find.TickManager.TicksGame;
        int remaining = GetRemainingTicksForPawnCore(p);
        if (remaining > 0)
        {
            // 重复注射：已有吸收延迟时，剩余时间减半；本剂与已有条目统一在新时间点生效�?
            int newEndTick = now + Mathf.Max(1, remaining / 2);
            foreach (var e in pending)
            {
                if (e.pawn == p) e.endTick = newEndTick;
            }
            endTick = newEndTick;
        }
        pending.Add(new PendingLactatingEntry { pawn = p, rawSeverity = rawSeverity, endTick = endTick, toleranceBefore = toleranceBefore });
        if (MilkCumDefOf.EM_AbsorptionDelay != null && p.health?.hediffSet?.GetFirstHediffOfDef(MilkCumDefOf.EM_AbsorptionDelay) == null)
            p.health.AddHediff(MilkCumDefOf.EM_AbsorptionDelay, p.GetBreastOrChestPart());
    }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        if (pending == null || pending.Count == 0) return;
        int now = Find.TickManager.TicksGame;
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            var e = pending[i];
            if (e.pawn == null || e.pawn.Destroyed || (!e.pawn.Spawned && e.pawn.Dead))
            {
                pending.RemoveAt(i);
                continue;
            }
            if (now < e.endTick) continue;
            pending.RemoveAt(i);
            ApplyDelayedLactating(e.pawn, e.rawSeverity, e.toleranceBefore);
        }
    }

    private static void ApplyDelayedLactating(Pawn pawn, float rawSeverity, float toleranceBefore)
    {
        if (pawn?.health?.hediffSet == null) return;
        if (MilkCumDefOf.EM_AbsorptionDelay != null && pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_AbsorptionDelay) is Hediff absorptionHediff)
            pawn.health.RemoveHediff(absorptionHediff);
            // Δs = rawSeverity × E_tol(t_before)：已包含一次耐受削弱，进水时不再�?E�?.3 动物差异化：乘种族药物倍率�?
            float deltaS = rawSeverity * MilkCumSettings.GetProlactinToleranceFactor(toleranceBefore) * MilkCumSettings.GetRaceDrugDeltaSMultiplier(pawn);
        var hediff = pawn.health.GetOrAddHediff(HediffDefOf.Lactating, pawn.GetBreastOrChestPart()) as HediffWithComps;
        if (hediff?.comps == null) return;
        foreach (var c in hediff.comps)
        {
            if (c is HediffComp_EqualMilkingLactating comp)
            {
                comp.AddFromDrug(deltaS);
                break;
            }
        }
        // 10.8-4：药物生效时给愉悦记忆；大剂量时挂催乳素兴奋（高量心情由 EM_Prolactin_HighThought 显示�?
        ApplyProlactinMoodEffects(pawn, rawSeverity);
        // 首次药物泌乳成就类记忆（仅一次）
        if (MilkCumDefOf.EM_FirstLactationDrug != null && pawn.needs?.mood?.thoughts?.memories != null
            && !pawn.needs.mood.thoughts.memories.Memories.Any(m => m.def == MilkCumDefOf.EM_FirstLactationDrug))
            pawn.needs.mood.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_FirstLactationDrug);
    }

    /// <summary>10.8-4：药物生效后的心情效果（愉悦记忆 + 大剂量兴�?hediff），供延迟生效与�?World 立即生效共用</summary>
    public static void ApplyProlactinMoodEffects(Pawn pawn, float severity)
    {
        if (pawn == null) return;
        if (pawn.needs?.mood?.thoughts?.memories != null && MilkCumDefOf.EM_Prolactin_Joy != null)
            pawn.needs.mood.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_Prolactin_Joy);
        if (severity >= 2f && MilkCumDefOf.EM_Prolactin_High != null)
        {
            var high = pawn.health.GetOrAddHediff(MilkCumDefOf.EM_Prolactin_High, pawn.GetBreastOrChestPart());
            if (high.Severity < 1f) high.Severity = 1f;
        }
    }

    /// <summary>根据代谢率计算吸收延�?tick。用 Lerp 映射 rate→倍率(0.5~1.5)，不做除法，避免 rate 极小或负值时爆炸。原�?MetabolicRate 可为 0.01、负数等</summary>
    public static int GetAbsorptionDelayTicks(Pawn pawn)
    {
        float rate = Mathf.Clamp(GetMetabolicRate(pawn), 0.25f, 2f);
        float factor = Mathf.Lerp(1.5f, 0.5f, Mathf.InverseLerp(0.25f, 2f, rate));
        return Mathf.Max(1, Mathf.RoundToInt(PoolModelConstants.BaseAbsorptionDelayTicks * factor));
    }

    /// <summary>该小人在待生效队列中最早一批的剩余 tick（用于悬停显示）。无待生效则返回 0</summary>
    public static int GetRemainingTicksForPawn(Pawn p)
    {
        if (p == null || Find.World == null) return 0;
        var comp = Find.World.GetComponent<WorldComponent_MilkCumAbsorptionDelay>();
        return comp?.GetRemainingTicksForPawnCore(p) ?? 0;
    }

    /// <summary>实例方法：该小人在本组件待生效队列中最早一批的剩余 tick</summary>
    private int GetRemainingTicksForPawnCore(Pawn p)
    {
        if (p == null || pending == null || pending.Count == 0) return 0;
        int now = Find.TickManager.TicksGame;
        int minRemaining = int.MaxValue;
        foreach (var e in pending)
        {
            if (e.pawn != p || e.pawn.Destroyed) continue;
            int remaining = e.endTick - now;
            if (remaining < minRemaining) minRemaining = remaining;
        }
        return minRemaining <= 0 ? 0 : minRemaining;
    }

    /// <summary>�?pawn 代谢率（原版 StatDef MetabolicRate，营养消耗倍率）。≤0、NaN、无 stat 时返�?1f。见 Docs/泌乳系统逻辑图</summary>
    private static float GetMetabolicRate(Pawn pawn)
    {
        if (pawn == null) return 1f;
        StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail("MetabolicRate");
        if (stat == null) return 1f;
        float v = pawn.GetStatValue(stat);
        if (float.IsNaN(v) || float.IsInfinity(v) || v <= 0f) return 1f;
        return v;
    }
}
