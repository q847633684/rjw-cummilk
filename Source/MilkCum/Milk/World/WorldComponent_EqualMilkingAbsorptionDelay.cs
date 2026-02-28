using System.Collections.Generic;
using MilkCum.Core;
using MilkCum.Milk.Helpers;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;

namespace MilkCum.Milk.World;

/// <summary>吸收延迟待生效条目：到 endTick 时给 pawn 添加 Lactating 并执行 AddFromDrug(deltaSeverity)。见 Docs/泌乳系统逻辑图。</summary>
public class PendingLactatingEntry : IExposable
{
    public Pawn pawn;
    public float rawSeverity;
    public int endTick;
    /// <summary>吃药前的耐受严重度 t_before，用于计算 Δs 与 E_tol。</summary>
    public float toleranceBefore;

    public void ExposeData()
    {
        Scribe_References.Look(ref pawn, "pawn");
        Scribe_Values.Look(ref rawSeverity, "severity", 0f);
        Scribe_Values.Look(ref endTick, "endTick", 0);
        Scribe_Values.Look(ref toleranceBefore, "toleranceBefore", 0f);
    }
}

/// <summary>水池模型吸收延迟：吃药后延迟一段时间再挂 Lactating 并进水，延迟由代谢率决定。见 Docs/泌乳系统逻辑图。</summary>
public class WorldComponent_EqualMilkingAbsorptionDelay : WorldComponent
{
    private List<PendingLactatingEntry> pending = new List<PendingLactatingEntry>();

    public WorldComponent_EqualMilkingAbsorptionDelay(global::RimWorld.Planet.World world) : base(world) { }

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

    /// <summary>加载后移除 pawn 为 null 或已销毁的条目，避免跨存档/地图引用。见 Docs/泌乳系统逻辑图。</summary>
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
        pending.Add(new PendingLactatingEntry { pawn = p, rawSeverity = rawSeverity, endTick = endTick, toleranceBefore = toleranceBefore });
        if (EMDefOf.EM_AbsorptionDelay != null && p.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_AbsorptionDelay) == null)
            p.health.AddHediff(EMDefOf.EM_AbsorptionDelay);
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
        if (EMDefOf.EM_AbsorptionDelay != null && pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_AbsorptionDelay) is Hediff absorptionHediff)
            pawn.health.RemoveHediff(absorptionHediff);
            // Δs = rawSeverity × E_tol(t_before)：已包含一次耐受削弱，进水时不再乘 E。3.3 动物差异化：乘种族药物倍率。
            float deltaS = rawSeverity * EqualMilkingSettings.GetProlactinToleranceFactor(toleranceBefore) * EqualMilkingSettings.GetRaceDrugDeltaSMultiplier(pawn);
        var hediff = pawn.health.GetOrAddHediff(HediffDefOf.Lactating) as HediffWithComps;
        if (hediff?.comps == null) return;
        foreach (var c in hediff.comps)
        {
            if (c is HediffComp_EqualMilkingLactating comp)
            {
                comp.AddFromDrug(deltaS);
                break;
            }
        }
        // 10.8-4：药物生效时给愉悦记忆；大剂量时挂催乳素兴奋（高量心情由 EM_Prolactin_HighThought 显示）
        ApplyProlactinMoodEffects(pawn, rawSeverity);
    }

    /// <summary>10.8-4：药物生效后的心情效果（愉悦记忆 + 大剂量兴奋 hediff），供延迟生效与无 World 立即生效共用。</summary>
    public static void ApplyProlactinMoodEffects(Pawn pawn, float severity)
    {
        if (pawn == null) return;
        if (pawn.needs?.mood?.thoughts?.memories != null && EMDefOf.EM_Prolactin_Joy != null)
            pawn.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_Prolactin_Joy);
        if (severity >= 2f && EMDefOf.EM_Prolactin_High != null)
        {
            var high = pawn.health.GetOrAddHediff(EMDefOf.EM_Prolactin_High);
            if (high.Severity < 1f) high.Severity = 1f;
        }
    }

    /// <summary>根据代谢率计算吸收延迟 tick。代谢率高则延迟短。</summary>
    public static int GetAbsorptionDelayTicks(Pawn pawn)
    {
        float rate = GetMetabolicRate(pawn);
        rate = Mathf.Clamp(rate, 0.25f, 2f);
        return Mathf.Max(1, (int)(PoolModelConstants.BaseAbsorptionDelayTicks / rate));
    }

    /// <summary>该小人在待生效队列中最早一批的剩余 tick（用于悬停显示）。无待生效则返回 0。</summary>
    public static int GetRemainingTicksForPawn(Pawn p)
    {
        if (p == null || Find.World == null) return 0;
        var comp = Find.World.GetComponent<WorldComponent_EqualMilkingAbsorptionDelay>();
        return comp?.GetRemainingTicksForPawnCore(p) ?? 0;
    }

    /// <summary>实例方法：该小人在本组件待生效队列中最早一批的剩余 tick。</summary>
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

    private static float GetMetabolicRate(Pawn pawn)
    {
        if (pawn == null) return 1f;
        var stat = DefDatabase<StatDef>.GetNamedSilentFail("MetabolicRate");
        if (stat != null)
            return pawn.GetStatValue(stat);
        return 1f;
    }
}
