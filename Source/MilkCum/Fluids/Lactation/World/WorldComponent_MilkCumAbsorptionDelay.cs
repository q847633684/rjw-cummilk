using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MilkCum.Core;
using MilkCum.Core.Settings;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;

namespace MilkCum.Fluids.Lactation.World;

/// <summary>吸收延迟待生效条目：到 endTick 时给 pawn 添加 Lactating 并执行 AddFromDrug(effectiveSeverity)。使用游戏按耐受削弱后的 effectiveSeverity 作为 Δs。</summary>
public class PendingLactatingEntry : IExposable
{
    public Pawn pawn;
    public float effectiveSeverity;
    public int endTick;

    public void ExposeData()
    {
        Scribe_References.Look(ref pawn, "pawn");
        Scribe_Values.Look(ref effectiveSeverity, "effectiveSeverity", 0f);
        Scribe_Values.Look(ref endTick, "endTick", 0);
    }
}

/// <summary>水池模型吸收延迟：吃药后延迟一段时间再�?Lactating 并进水，延迟由代谢率决定。见 记忆库/docs/泌乳系统逻辑图</summary>
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

    /// <summary>待生效队列中应丢弃的 pawn：无引用、已销毁、或未生成且已死亡。</summary>
    private static bool IsInvalidPendingPawn(Pawn p) =>
        p == null || p.Destroyed || (!p.Spawned && p.Dead);

    /// <summary>加载后移除无效条目，避免跨存档/地图坏引用。见 记忆库/docs/泌乳系统逻辑图</summary>
    private void RemoveInvalidPendingEntries()
    {
        if (pending == null) return;
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            if (IsInvalidPendingPawn(pending[i].pawn))
                pending.RemoveAt(i);
        }
    }

    public void ScheduleLactating(Pawn p, float effectiveSeverity, int endTick)
    {
        if (p == null || endTick <= 0) return;
        if (pending == null) pending = new List<PendingLactatingEntry>();
        int now = Find.TickManager.TicksGame;
        int remaining = GetRemainingTicksForPawnCore(p);
        if (remaining > 0)
        {
            int newEndTick = now + Mathf.Max(1, remaining / 2);
            foreach (var e in pending)
            {
                if (e.pawn == p) e.endTick = newEndTick;
            }
            endTick = newEndTick;
        }
        pending.Add(new PendingLactatingEntry { pawn = p, effectiveSeverity = effectiveSeverity, endTick = endTick });
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
            if (IsInvalidPendingPawn(e.pawn))
            {
                pending.RemoveAt(i);
                continue;
            }
            if (now < e.endTick) continue;
            pending.RemoveAt(i);
            ApplyDelayedLactating(e.pawn, e.effectiveSeverity);
        }
    }

    private static void ApplyDelayedLactating(Pawn pawn, float effectiveSeverity)
    {
        if (pawn?.health?.hediffSet == null) return;
        if (MilkCumDefOf.EM_AbsorptionDelay != null && pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_AbsorptionDelay) is Hediff absorptionHediff)
            pawn.health.RemoveHediff(absorptionHediff);
        float deltaS = effectiveSeverity;
        int tick = Find.TickManager.TicksGame;
        MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] pawn={pawn?.LabelShort} tick={tick} mode=DelayedApply Δs={deltaS:F3} effective={effectiveSeverity:F3}");
        MilkCumSettings.LactationLog($"[MilkCum][INFO][LactationDrug] 公式 泌乳增量Δs=队列内effectiveSeverity({effectiveSeverity:F3})={deltaS:F3}");
        try
        {
            BodyPartRecord bodyPart = null;
            try { bodyPart = pawn.GetBreastOrChestPart(); }
            catch (System.Exception ex)
            {
                MilkCumSettings.LactationLog($"GetBreastOrChestPart failed: {ex.GetType().Name}: {ex.Message}");
            }
            var hediff = pawn.health.GetOrAddHediff(HediffDefOf.Lactating, bodyPart) as HediffWithComps;
            if (hediff?.comps == null) return;
            foreach (var c in hediff.comps)
            {
                if (c is HediffComp_EqualMilkingLactating comp)
                {
                    comp.AddFromDrug(deltaS);
                    break;
                }
            }
            // 10.8-4：药物生效时给愉悦记忆；大剂量时挂催乳素兴奋
            ApplyProlactinMoodEffects(pawn, effectiveSeverity);
            // 首次药物泌乳成就类记忆（仅一次）
            if (MilkCumDefOf.EM_FirstLactationDrug != null && pawn.needs?.mood?.thoughts?.memories != null
                && !pawn.needs.mood.thoughts.memories.Memories.Any(m => m.def == MilkCumDefOf.EM_FirstLactationDrug))
                pawn.needs.mood.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_FirstLactationDrug);
            TryRecordProlactinIngestion(pawn);
        }
        catch (System.Exception ex)
        {
            Verse.Log.Error($"[MilkCum.Lactation] Prolactin delayed apply failed for {pawn?.Name}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
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

    /// <summary>挂接原版历史事件：若存在服药/成瘾相关 HistoryEventDef 则记录。原版事件名因版本可能不同，尝试常见名称；RecordEvent 通过 Reflection 调用以兼容不同 RimWorld 版本。</summary>
    internal static void TryRecordProlactinIngestion(Pawn pawn)
    {
        if (pawn == null) return;
        try
        {
            HistoryEventDef def = DefDatabase<HistoryEventDef>.GetNamedSilentFail("IngestedDrug")
                ?? DefDatabase<HistoryEventDef>.GetNamedSilentFail("UsedDrug")
                ?? DefDatabase<HistoryEventDef>.GetNamedSilentFail("DrugIngestion");
            if (def == null) return;
            HistoryEvent ev = new HistoryEvent(def, new NamedArgument(pawn, "doer"));
            object history = Find.History;
            if (history != null)
                history.GetType().GetMethod("RecordEvent", new[] { typeof(HistoryEvent) })?.Invoke(history, new object[] { ev });
        }
        catch (System.Exception) { /* 忽略版本差异 */ }
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

    /// <summary>�?pawn 代谢率（原版 StatDef MetabolicRate，营养消耗倍率）。≤0、NaN、无 stat 时返�?1f。见 记忆库/docs/泌乳系统逻辑图</summary>
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
