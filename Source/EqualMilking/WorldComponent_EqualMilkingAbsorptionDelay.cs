using System.Collections.Generic;
using EqualMilking.Helpers;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;

namespace EqualMilking
{
    /// <summary>吸收延迟待生效条目：到 endTick 时给 pawn 添加 Lactating 并执行 AddFromDrug(severity)。</summary>
    public class PendingLactatingEntry : IExposable
    {
        public Pawn pawn;
        public float severity;
        public int endTick;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref severity, "severity", 0f);
            Scribe_Values.Look(ref endTick, "endTick", 0);
        }
    }

    /// <summary>水池模型吸收延迟：吃药后延迟一段时间再挂 Lactating 并进水，延迟由代谢率决定。</summary>
    public class WorldComponent_EqualMilkingAbsorptionDelay : WorldComponent
    {
        private List<PendingLactatingEntry> pending = new List<PendingLactatingEntry>();

        public WorldComponent_EqualMilkingAbsorptionDelay(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pending, "pending", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && pending == null)
                pending = new List<PendingLactatingEntry>();
        }

        public void ScheduleLactating(Pawn p, float severity, int endTick)
        {
            if (p == null || endTick <= 0) return;
            if (pending == null) pending = new List<PendingLactatingEntry>();
            pending.Add(new PendingLactatingEntry { pawn = p, severity = severity, endTick = endTick });
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (pending == null || pending.Count == 0) return;
            int now = Find.TickManager.TicksGame;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var e = pending[i];
                if (e.pawn == null || !e.pawn.Spawned && e.pawn.Dead)
                {
                    pending.RemoveAt(i);
                    continue;
                }
                if (now < e.endTick) continue;
                pending.RemoveAt(i);
                ApplyDelayedLactating(e.pawn, e.severity);
            }
        }

        private static void ApplyDelayedLactating(Pawn pawn, float severity)
        {
            if (pawn?.health?.hediffSet == null) return;
            var hediff = pawn.health.GetOrAddHediff(HediffDefOf.Lactating) as HediffWithComps;
            if (hediff?.comps == null) return;
            foreach (var c in hediff.comps)
            {
                if (c is HediffComp_EqualMilkingLactating comp)
                {
                    comp.AddFromDrug(severity);
                    break;
                }
            }
            // 10.8-4：药物生效时给愉悦记忆；大剂量时挂催乳素兴奋（高量心情由 EM_Prolactin_HighThought 显示）
            ApplyProlactinMoodEffects(pawn, severity);
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

        private static float GetMetabolicRate(Pawn pawn)
        {
            if (pawn == null) return 1f;
            var stat = DefDatabase<StatDef>.GetNamedSilentFail("MetabolicRate");
            if (stat != null)
                return pawn.GetStatValue(stat);
            return 1f;
        }
    }
}
