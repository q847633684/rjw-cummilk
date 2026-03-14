using System.Collections.Generic;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Comps;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Comps;

/// <summary>挤奶流速与产奶：单侧/总流速、SpawnBottles、PlaceMilkThingOnce。见 Docs/泌乳系统逻辑图。</summary>
public partial class CompEquallyMilkable
{
    /// <summary>单侧流速（池单位/秒），index 为 GetCachedEntries() 下标。吸奶只关心一侧时用此方法避免分配全表。</summary>
    private float GetMilkingFlowRateForSideIndex(int index, bool isMachine, Building_Milking building)
    {
        float baseFlowPerSecond = 60f / Mathf.Max(0.01f, MilkCumSettings.milkingWorkTotalBase);
        float raceMult = MilkCumSettings.GetRaceDrugDeltaSMultiplier(Pawn);
        var lactatingComp = Pawn?.LactatingHediffComp();
        breastFullness ??= new Dictionary<string, float>();
        var entries = GetCachedEntries();
        float speedMult = 1f + (isMachine ? (building?.SpeedOffset() ?? 0f) : 0f);
        speedMult = Mathf.Max(0.01f, speedMult);
        if (lactatingComp == null)
            return baseFlowPerSecond * raceMult * speedMult;
        if (entries.Count == 0)
        {
            float letdown = Mathf.Max(0.01f, lactatingComp.GetLetdownReflexFlowMultiplier());
            float maxF = Mathf.Max(0.01f, maxFullness);
            float f = Mathf.Clamp01(Fullness / maxF);
            return baseFlowPerSecond * raceMult * (f * f) * letdown * speedMult;
        }
        if (index < 0 || index >= entries.Count) return 0f;
        var e = entries[index];
        if (string.IsNullOrEmpty(e.Key)) return 0f;
        float fullness = GetFullnessForKey(e.Key);
        float cap = Mathf.Max(0.01f, e.Capacity);
        float fSide = Mathf.Clamp01(fullness / cap);
        float letdownSide = Mathf.Max(0.01f, lactatingComp.GetLetdownReflexFlowMultiplier(e.Key));
        return baseFlowPerSecond * raceMult * (fSide * fSide) * letdownSide * speedMult;
    }

    /// <summary>挤奶时的挤出流速（池单位/秒）：各侧按「该侧满度/该侧容量」算挤出乳压 f² 与该侧 letdown，求和后×基准×种族×机器倍率。手挤用总流速；机器挤用 GetMilkingFlowRatesPerSide 每侧独立、并行。</summary>
    public float GetMilkingFlowRate(bool isMachine, Building_Milking building = null)
    {
        var perSide = GetMilkingFlowRatesPerSide(isMachine, building);
        float sum = 0f;
        for (int i = 0; i < perSide.Count; i++)
            sum += perSide[i];
        return sum;
    }

    /// <summary>各侧的挤出流速（池单位/秒），与 GetCachedEntries() 顺序一致。手挤用其和；机器挤用每侧独立排空、并行，总耗时由最慢一侧决定。无分侧时返回单元素列表。</summary>
    public List<float> GetMilkingFlowRatesPerSide(bool isMachine, Building_Milking building = null)
    {
        var entries = GetCachedEntries();
        int n = entries.Count > 0 ? entries.Count : 1;
        var list = new List<float>(n);
        for (int i = 0; i < n; i++)
            list.Add(GetMilkingFlowRateForSideIndex(i, isMachine, building));
        return list;
    }

    /// <summary>吸奶专用：按「即将被吸的那一侧」的流速，选侧规则与 DrainForConsume(..., singleSideOnly: true) 一致；只算单侧不分配全表。</summary>
    public float GetMilkingFlowRateForSingleSide()
    {
        int idx = GetFirstDrainSideIndex();
        return GetMilkingFlowRateForSideIndex(idx, false, null);
    }

    /// <summary>放置一个奶物品并设置产主信息；整瓶与未满瓶共用，减少重复。</summary>
    private static void PlaceMilkThingOnce(Thing thing, Pawn producer, Building_Milking milkingSpot, Pawn doer)
    {
        if (thing.TryGetComp<CompShowProducer>() is CompShowProducer compShowProducer && producer != null && producer.RaceProps.Humanlike)
        {
            if (MilkCumSettings.HasRaceTag(thing))
                compShowProducer.producerKind = producer.kindDef;
            if (MilkCumSettings.HasPawnTag(thing))
                compShowProducer.producer = producer;
        }
        if (milkingSpot != null)
            milkingSpot.PlaceMilkThing(thing);
        else
            GenPlace.TryPlaceThing(thing, doer.Position, doer.Map, ThingPlaceMode.Near);
    }

    /// <summary>流速驱动挤奶：根据已扣池总量生成奶瓶并处理心情/乳腺炎等（不再次扣池）。由 JobDriver 按速率逐 tick 扣池后调用。1 池单位 = 1 瓶，不做采集加成。</summary>
    public void SpawnBottlesForDrainedAmount(float totalDrained, Pawn doer, Building_Milking milkingSpot = null)
    {
        if (totalDrained <= 0f)
        {
            SyncBaseFullness();
            return;
        }
        Pawn pawn = parent as Pawn;
        int tick = Find.TickManager.TicksGame;
        float fullnessBeforeTotal = Fullness;
        pawn?.LactatingHediffWithComps()?.TryGetComp<HediffComp_EqualMilkingLactating>()?.AddRemainingDays(1f);
        int numBottles = Mathf.FloorToInt(totalDrained);
        if (totalDrained - numBottles >= 0.999f)
            numBottles++;
        int bottlesSpawned = 0;
        if (numBottles > 0)
        {
            pawn.LactatingHediffWithComps()?.OnGathered();
            int num = numBottles;
            while (num > 0)
            {
                int stack = Mathf.Clamp(num, 1, ResourceDef.stackLimit);
                num -= stack;
                Thing thing = ThingMaker.MakeThing(ResourceDef);
                thing.stackCount = stack;
                PlaceMilkThingOnce(thing, pawn, milkingSpot, doer);
                bottlesSpawned += stack;
            }
        }
        else if (totalDrained >= 0.001f && totalDrained < 1f && MilkCumDefOf.EM_HumanMilkPartial != null && ResourceDef == MilkCumDefOf.EM_HumanMilk)
        {
            pawn.LactatingHediffWithComps()?.OnGathered();
            Thing thing = ThingMaker.MakeThing(MilkCumDefOf.EM_HumanMilkPartial);
            if (thing.TryGetComp<CompPartialMilk>() is CompPartialMilk compPartial)
                compPartial.fillAmount = totalDrained;
            PlaceMilkThingOnce(thing, pawn, milkingSpot, doer);
            bottlesSpawned = 1;
        }
        SyncBaseFullness();
        if (MilkCumSettings.milkingActionLog && pawn != null && totalDrained > 0f)
        {
            float fullnessAfterTotal = Fullness;
            MilkCumSettings.LactationLog($"[MilkCum][INFO][Milking] pawn={pawn.LabelShort} tick={tick} mode=SpawnBottles drained={totalDrained:F3} bottlesSpawned={bottlesSpawned} fullnessBefore={fullnessBeforeTotal:F3} fullnessAfter={fullnessAfterTotal:F3} doer={doer?.LabelShort}");
        }
        if (parent is Pawn milkedPawn && milkedPawn.RaceProps.Humanlike && milkedPawn.needs?.mood?.thoughts?.memories != null)
        {
            if (MilkPermissionExtensions.IsAllowedSuckler(milkedPawn, doer))
            {
                if (MilkCumDefOf.EM_AllowedMilking != null)
                    milkedPawn.needs.mood.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_AllowedMilking);
                if (milkedPawn.HasDrugInducedLactation() && MilkCumDefOf.EM_Prolactin_Joy != null)
                    milkedPawn.needs.mood.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_Prolactin_Joy);
            }
            else if (MilkCumDefOf.EM_ForcedMilking != null)
                milkedPawn.needs.mood.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_ForcedMilking);
        }
        lastGatheredTick = Find.TickManager.TicksGame;
        if (parent is Pawn pawnForMastitis && MilkCumDefOf.EM_Mastitis != null)
        {
            var mastitis = pawnForMastitis.health?.hediffSet?.GetFirstHediffOfDef(MilkCumDefOf.EM_Mastitis);
            if (mastitis != null && mastitis.Severity > 0.01f)
            {
                mastitis.Severity = Mathf.Max(0f, mastitis.Severity - 0.05f);
                if (mastitis.Severity <= 0f)
                    pawnForMastitis.health.RemoveHediff(mastitis);
            }
        }
    }
}
