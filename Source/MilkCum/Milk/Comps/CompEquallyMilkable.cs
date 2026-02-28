using Verse;
using RimWorld;
using UnityEngine;
using MilkCum.Core;
using MilkCum.Milk.Helpers;
using System.Collections.Generic;
using System.Linq;
using MilkCum.Milk.Data;

namespace MilkCum.Milk.Comps;
public class CompEquallyMilkable : CompMilkable
{
    protected Pawn Pawn => parent as Pawn;
    protected override int GatherResourcesIntervalDays => Mathf.Max((int)Props.milkIntervalDays, 1);
    protected override int ResourceAmount => (int)Pawn.MilkAmount();
    protected override ThingDef ResourceDef => Pawn.MilkDef();
    protected virtual float fResourceAmount => Pawn.MilkAmount();
    protected virtual float GrowthMultiplier => Pawn.MilkGrowthMultiplier();
    public float breastfedAmount = 0f;
    public float maxFullness = 1f;

    /// <summary>水池模型：左乳水位（0～左乳基础容量），与右乳合计 0～1；容量由种族×乳房大小按部位区分。</summary>
    private float leftFullness;
    /// <summary>水池模型：右乳水位（0～右乳基础容量），与左乳合计 0～1。</summary>
    private float rightFullness;
    /// <summary>旧实现与旧存档兼容：固定单侧容量 0.5；当前容量由 GetLeft/RightBreastCapacityFactor 决定。</summary>
    private const float HalfPool = 0.5f;

    /// <summary>当前总奶量（0~maxFullness），双池时 = leftFullness + rightFullness；对外只读。</summary>
    public new float Fullness => leftFullness + rightFullness;
    public float LeftFullness => leftFullness;
    public float RightFullness => rightFullness;
    private MilkSettings milkSettings = null;
    public MilkSettings MilkSettings
    {
        get
        {
            if (milkSettings == null && parent is Pawn pawn)
            {
                milkSettings = pawn.GetDefaultMilkSetting();
            }
            return milkSettings;
        }
    }
    internal List<Pawn> assignedFeeders = new();
    /// <summary>谁可以使用我的奶。名单为空时会预填子女+伴侣；仅名单内的人可吸奶/挤奶。</summary>
    internal List<Pawn> allowedSucklers = new();
    /// <summary>谁可以使用我产出的奶/精液制品（不含自己，自己始终允许）。空列表 = 仅自己；非空 = 自己+列表中人。囚犯/奴隶产主时亦不默认允许殖民者（7.4）。</summary>
    internal List<Pawn> allowedConsumers = new();
    private int updateTick = 0;
    private bool cachedActive = false;
    /// <summary>满池溢出：累计溢出量，达到阈值时生成地面污物（不扣水位）。</summary>
    private float overflowAccumulator = 0f;
    /// <summary>10.8-6：最近一次被挤奶的游戏 tick，用于「长时间未挤奶」心情判定。</summary>
    private int lastGatheredTick = -1;
    /// <summary>规格：连续满池（Fullness≥0.95）的 tick 数，用于乳腺炎/堵塞触发（长时间满池）。</summary>
    private int ticksFullPool;
    /// <summary>满池溢出累计量（仅 Debug 显示用）。</summary>
    internal float OverflowAccumulator => overflowAccumulator;

    /// <summary>机器/设备兼容：下次应排的一侧（true=左，false=右）。与 Gathered 选侧一致：最满一侧，相同时男左女右。</summary>
    public bool GetPreferredDrainSideLeft()
    {
        bool preferLeft = Pawn?.gender == Gender.Male;
        return leftFullness > rightFullness || (Mathf.Approximately(leftFullness, rightFullness) && preferLeft);
    }

    /// <summary>机器/设备兼容：当前应排一侧的可排奶量（0～1，与该侧水位一致）。</summary>
    public float GetDrainableAmountOnPreferredSide()
    {
        return GetPreferredDrainSideLeft() ? leftFullness : rightFullness;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref breastfedAmount, "BreastfedAmount", 0f);
        Scribe_Values.Look(ref leftFullness, "PoolLeftFullness", 0f);
        Scribe_Values.Look(ref rightFullness, "PoolRightFullness", 0f);
        Scribe_Values.Look(ref overflowAccumulator, "PoolOverflowAccumulator", 0f);
        Scribe_Values.Look(ref lastGatheredTick, "PoolLastGatheredTick", -1);
        Scribe_Values.Look(ref ticksFullPool, "PoolTicksFull", 0);
        Scribe_Deep.Look(ref milkSettings, "MilkSettings");
        Scribe_Collections.Look(ref assignedFeeders, "CanBeFedBy", LookMode.Reference);
        Scribe_Collections.Look(ref allowedSucklers, "AllowedSucklers", LookMode.Reference);
        Scribe_Collections.Look(ref allowedConsumers, "AllowedConsumers", LookMode.Reference);
        // 旧存档兼容：无双池时用 base.fullness 均分
        if (Scribe.mode == LoadSaveMode.PostLoadInit && leftFullness <= 0f && rightFullness <= 0f && fullness > 0f)
        {
            leftFullness = rightFullness = Mathf.Clamp(fullness * 0.5f, 0f, HalfPool);
        }
        if (Scribe.mode == LoadSaveMode.PostLoadInit && lastGatheredTick < 0)
            lastGatheredTick = Find.TickManager.TicksGame;
        SyncBaseFullness();
    }

    /// <summary>10.8-6：最近一次被挤奶的游戏 tick；供 ThoughtWorker_LongTimeNotMilked 使用。</summary>
    public int LastGatheredTick => lastGatheredTick;

    /// <summary>将双池总和同步到基类 fullness，供可能读取基类字段的代码使用。</summary>
    private void SyncBaseFullness()
    {
        fullness = Mathf.Clamp(leftFullness + rightFullness, 0f, maxFullness);
    }
    /// <summary>泌乳结束时清空双池（由 HediffComp_EqualMilkingLactating 调用）。</summary>
    public void ClearPools()
    {
        leftFullness = 0f;
        rightFullness = 0f;
        SyncBaseFullness();
    }
    /// <summary>7.11: 旧存档兼容 — 确保列表非 null、移除无效引用；名单为空时预填子女+伴侣（默认勾选）。</summary>
    public void EnsureSaveCompatAllowedLists()
    {
        allowedSucklers ??= new List<Pawn>();
        allowedConsumers ??= new List<Pawn>();
        assignedFeeders ??= new List<Pawn>();
        allowedSucklers.RemoveAll(p => p == null || p.Destroyed);
        allowedConsumers.RemoveAll(p => p == null || p.Destroyed);
        assignedFeeders.RemoveAll(p => p == null || p.Destroyed);
        if (allowedSucklers.Count == 0 && parent is Pawn p)
        {
            var defaults = ExtensionHelper.GetDefaultSucklers(p);
            foreach (Pawn pawn in defaults)
                if (pawn != null && !pawn.Destroyed && !allowedSucklers.Contains(pawn))
                    allowedSucklers.Add(pawn);
        }
    }
    protected override bool Active
    {
        get
        {
            if (updateTick > Find.TickManager.TicksGame) { return cachedActive; }
            if (parent.Faction == null || parent is not Pawn pawn || !parent.SpawnedOrAnyParentSpawned || !pawn.IsColonyPawn())
            {
                updateTick = Find.TickManager.TicksGame + 500;
                cachedActive = false;
                return false;
            }
            // Add/Remove lactating hediff
            // Apply gene effect
            if (pawn.genes?.HasActiveGene(EMDefOf.EM_Permanent_Lactation) == true)
            {
                Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating);
                lactating.Severity = Mathf.Max(lactating.Severity, 0.9999f);
            }
            // Remove lactating hediff for non-milkable animals/mechs
            else if (!pawn.RaceProps.Humanlike && !pawn.IsMilkable())
            {
                if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating) is Hediff lactating)
                {
                    pawn.health.RemoveHediff(lactating);
                }
                cachedActive = false;
                updateTick = Find.TickManager.TicksGame + 500;
                return false;
            }
            // Add lactating hediff for lactating animals
            else if (EqualMilkingSettings.femaleAnimalAdultAlwaysLactating && pawn.IsAdultFemaleAnimalOfColony())
            {
                Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating);
                lactating.Severity = Mathf.Max(lactating.Severity, 1f);
            }

            if (pawn.IsLactating() && pawn.IsMilkable())
            {
                cachedActive = true;
                return true;
            }

            cachedActive = false;
            updateTick = Find.TickManager.TicksGame + 500;
            return false;
        }
    }
    public override void CompTick()
    {
        if (!parent.IsHashIntervalTick(30)) { return; }
        ApplyDrugInducedLactationEffects();
        if (!Active) { return; }
        UpdateMilkPools();
        if (parent.IsHashIntervalTick(2000)) TryTriggerMastitis();
        UpdateHealthHediffs();
    }

    /// <summary>药物诱发泌乳负担与增益 Hediff；不依赖 Active，以便停止泌乳或失去耐受/成瘾时能移除。</summary>
    private void ApplyDrugInducedLactationEffects()
    {
        if (EMDefOf.EM_DrugLactationBurden != null && Pawn != null && Pawn.RaceProps.Humanlike)
        {
            if (Pawn.IsLactating() && Pawn.HasDrugInducedLactation())
            {
                if (Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_DrugLactationBurden) == null)
                    Pawn.health.AddHediff(EMDefOf.EM_DrugLactationBurden, Pawn.GetBreastOrChestPart());
            }
            else if (Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_DrugLactationBurden) is Hediff burden)
                Pawn.health.RemoveHediff(burden);
        }
        if (EMDefOf.EM_LactatingGain != null && Pawn != null && Pawn.RaceProps.Humanlike)
        {
            if (Pawn.IsLactating() && Pawn.HasDrugInducedLactation() && EqualMilkingSettings.lactatingGainEnabled && EqualMilkingSettings.lactatingGainCapModPercent > 0f)
            {
                if (Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_LactatingGain) == null)
                    Pawn.health.AddHediff(EMDefOf.EM_LactatingGain, Pawn.GetBreastOrChestPart());
            }
            else if (Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_LactatingGain) is Hediff gain)
                Pawn.health.RemoveHediff(gain);
        }
    }

    /// <summary>水池模型：按容量比例分配流速，双池进水、溢出、回缩（含健康度系数），更新满池计数。</summary>
    private void UpdateMilkPools()
    {
        var lactatingComp = Pawn?.LactatingHediffWithComps()?.comps?.OfType<HediffComp_EqualMilkingLactating>().FirstOrDefault();
        if (lactatingComp == null || lactatingComp.RemainingDays <= 0f) { return; }
        float currentLactation = lactatingComp.CurrentLactationAmount;
        float hungerFactor = PawnUtility.BodyResourceGrowthSpeed(Pawn);
        if (currentLactation <= 0f || hungerFactor <= 0f) { return; }
        float flowPerDay = currentLactation * hungerFactor;
        flowPerDay *= Pawn.GetMilkFlowMultiplierFromConditions();
        flowPerDay *= Pawn.GetMilkFlowMultiplierFromGenes();
        flowPerDay *= (Pawn.RaceProps.Humanlike ? EqualMilkingSettings.defaultFlowMultiplierForHumanlike : 1f);
        float flowPerTick = flowPerDay / 60000f * 30f;

        float leftBaseCap = Pawn.GetLeftBreastCapacityFactor();
        float rightBaseCap = Pawn.GetRightBreastCapacityFactor();
        float totalBaseCap = leftBaseCap + rightBaseCap;
        float flowLeftPerTick;
        float flowRightPerTick;
        if (totalBaseCap < 1E-6f)
        {
            flowLeftPerTick = flowPerTick * 0.5f;
            flowRightPerTick = flowPerTick * 0.5f;
        }
        else
        {
            flowLeftPerTick = flowPerTick * (leftBaseCap / totalBaseCap);
            flowRightPerTick = flowPerTick * (rightBaseCap / totalBaseCap);
        }

        float stretchCapLeft = leftBaseCap * PoolModelConstants.StretchCapFactor;
        float stretchCapRight = rightBaseCap * PoolModelConstants.StretchCapFactor;
        var pool = new LactationPoolState();
        pool.SetFrom(leftFullness, rightFullness, ticksFullPool);
        float overflowTotal = pool.TickGrowth(flowLeftPerTick, flowRightPerTick, stretchCapLeft, stretchCapRight);
        float healthPercent = 1f;
        if (Pawn?.health?.summaryHealth != null)
            healthPercent = Mathf.Clamp(Pawn.health.summaryHealth.SummaryHealthPercent, 0.2f, 1f);
        float shrinkFactor = (1f - PoolModelConstants.ShrinkPerStep) * healthPercent;
        pool.TickShrink(leftBaseCap, rightBaseCap, shrinkFactor);
        pool.UpdateFullPoolCounter(0.95f, 30);

        leftFullness = pool.LeftFullness;
        rightFullness = pool.RightFullness;
        ticksFullPool = pool.TicksFullPool;
        SyncBaseFullness();
        HandleOverflow(overflowTotal);
    }

    /// <summary>满池溢出：累计溢出量达到阈值时生成地面污物（不扣水位）；污物 Def 由设置指定。</summary>
    private void HandleOverflow(float overflowThisTick)
    {
        if (overflowThisTick <= 0f || Pawn == null || !Pawn.Spawned || Pawn.Map == null) { return; }
        overflowAccumulator += overflowThisTick;
        var filthDef = string.IsNullOrEmpty(EqualMilkingSettings.overflowFilthDefName)
            ? DefDatabase<ThingDef>.GetNamedSilentFail("Filth_Vomit")
            : DefDatabase<ThingDef>.GetNamedSilentFail(EqualMilkingSettings.overflowFilthDefName);
        if (filthDef == null)
            filthDef = DefDatabase<ThingDef>.GetNamedSilentFail("Filth_Vomit");
        if (filthDef != null)
        {
            int filthSpawned = 0;
            while (overflowAccumulator >= PoolModelConstants.OverflowFilthThreshold)
            {
                FilthMaker.TryMakeFilth(Pawn.Position, Pawn.Map, filthDef, 1);
                overflowAccumulator -= PoolModelConstants.OverflowFilthThreshold;
                filthSpawned++;
            }
            if (filthSpawned > 0 && Pawn.RaceProps.Humanlike && Pawn.needs?.mood?.thoughts?.memories != null
                && EMDefOf.EM_MilkOverflow != null)
                Pawn.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_MilkOverflow);
            if (filthSpawned > 0 && Pawn.Spawned && Pawn.Map != null)
            {
                string moteText = "EM.MilkOverflowMote".Translate();
                if (!string.IsNullOrEmpty(moteText) && moteText != "EM.MilkOverflowMote")
                    MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, moteText, 2.5f);
            }
        }
        overflowAccumulator = Mathf.Min(overflowAccumulator, PoolModelConstants.OverflowFilthThreshold * 2f);
    }

    /// <summary>满池时添加「乳房胀满」hediff；低于 0.9 时移除（滞后避免抖动）。</summary>
    private void UpdateHealthHediffs()
    {
        if (EMDefOf.EM_BreastsEngorged == null || Pawn == null || !Pawn.RaceProps.Humanlike || !Pawn.IsLactating()) return;
        var engorged = Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_BreastsEngorged);
        if (Fullness >= 0.95f && engorged == null)
            Pawn.health.AddHediff(EMDefOf.EM_BreastsEngorged, Pawn.GetBreastOrChestPart());
        else if (Fullness < 0.9f && engorged != null)
            Pawn.health.RemoveHediff(engorged);
    }
    /// <summary>
    /// 设置总奶量（0～1）。从双池中排出至目标值，先排最满一侧，相同时男左女右。
    /// </summary>
    public void SetFullness(float value)
    {
        float target = Mathf.Clamp(value, 0f, maxFullness);
        float total = leftFullness + rightFullness;
        if (target >= total) { SyncBaseFullness(); return; }
        float toDrain = total - target;
        bool preferLeft = Pawn?.gender == Gender.Male; // 男左女右：男先左，女先右
        if (leftFullness > rightFullness || (Mathf.Approximately(leftFullness, rightFullness) && preferLeft))
        {
            float fromLeft = Mathf.Min(leftFullness, toDrain);
            leftFullness -= fromLeft;
            toDrain -= fromLeft;
            if (toDrain > 0f)
                rightFullness = Mathf.Max(0f, rightFullness - toDrain);
        }
        else
        {
            float fromRight = Mathf.Min(rightFullness, toDrain);
            rightFullness -= fromRight;
            toDrain -= fromRight;
            if (toDrain > 0f)
                leftFullness = Mathf.Max(0f, leftFullness - toDrain);
        }
        SyncBaseFullness();
    }
    /// <summary>
    /// 挤奶：先挤最满一侧，相同时男左女右；只排空该侧并产出奶。
    /// </summary>
    new public void Gathered(Pawn doer)
    {
        if (!Active)
        {
            Log.Error(string.Concat(doer, " gathered body resources while not Active: ", parent));
        }
        Pawn pawn = parent as Pawn;
        bool preferLeft = pawn?.gender == Gender.Male;
        bool drainLeft = leftFullness > rightFullness || (Mathf.Approximately(leftFullness, rightFullness) && preferLeft);
        float sideFullness = drainLeft ? leftFullness : rightFullness;
        if (sideFullness <= 0f)
        {
            SyncBaseFullness();
            return;
        }
        float yieldFactor = doer.GetStatValue(StatDefOf.AnimalGatherYield);
        Building_Milking milkingSpot = (doer.jobs?.curDriver as JobDriver_EquallyMilk)?.MilkBuilding;
        if (milkingSpot != null)
            yieldFactor += milkingSpot.YieldOffset();
        if (!Rand.Chance(yieldFactor) && parent.Map != null)
        {
            MoteMaker.ThrowText((doer.DrawPos + parent.DrawPos) / 2f, parent.Map, Lang.ProductWasted, 3.65f);
        }
        else
        {
            int num = GenMath.RoundRandom(fResourceAmount * sideFullness);
            pawn.LactatingHediffWithComps()?.OnGathered();
            while (num > 0)
            {
                int stack = Mathf.Clamp(num, 1, ResourceDef.stackLimit);
                num -= stack;
                Thing thing = ThingMaker.MakeThing(ResourceDef);
                if (thing.TryGetComp<CompShowProducer>() is CompShowProducer compShowProducer && pawn.RaceProps.Humanlike)
                {
                    if (EqualMilkingSettings.HasRaceTag(thing))
                        compShowProducer.producerKind = pawn.kindDef;
                    if (EqualMilkingSettings.HasPawnTag(thing))
                        compShowProducer.producer = pawn;
                }
                thing.stackCount = stack;
                if (milkingSpot != null)
                    milkingSpot.PlaceMilkThing(thing);
                else
                    GenPlace.TryPlaceThing(thing, doer.Position, doer.Map, ThingPlaceMode.Near);
            }
        }
        if (drainLeft)
            leftFullness = 0f;
        else
            rightFullness = 0f;
        SyncBaseFullness();
        // 10.8-5：挤奶心情细分 — 被允许的人挤奶给正面记忆，否则给强制挤奶负面记忆
        if (parent is Pawn producer && producer.RaceProps.Humanlike && producer.needs?.mood?.thoughts?.memories != null)
        {
            if (ExtensionHelper.IsAllowedSuckler(producer, doer))
            {
                if (EMDefOf.EM_AllowedMilking != null)
                    producer.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_AllowedMilking);
                if (producer.HasDrugInducedLactation() && EMDefOf.EM_Prolactin_Joy != null)
                    producer.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_Prolactin_Joy);
            }
            else if (EMDefOf.EM_ForcedMilking != null)
                producer.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_ForcedMilking);
        }
        // 10.8-6：记录最近一次挤奶时刻，供「长时间未挤奶」心情判定
        lastGatheredTick = Find.TickManager.TicksGame;
        // 规格：挤奶时略微缓解乳房不适/乳腺炎
        if (parent is Pawn producer && EMDefOf.EM_Mastitis != null)
        {
            var mastitis = producer.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_Mastitis);
            if (mastitis != null && mastitis.Severity > 0.01f)
            {
                mastitis.Severity = Mathf.Max(0f, mastitis.Severity - 0.05f);
                if (mastitis.Severity <= 0f)
                    producer.health.RemoveHediff(mastitis);
            }
        }
    }

    /// <summary>规格：乳腺炎/堵塞可由长时间满池、卫生、受伤等触发。每 2000 tick 判定一次；参数由 EqualMilkingSettings 可调；营养好则 MTB 延长（风险降低）。</summary>
    private void TryTriggerMastitis()
    {
        if (EMDefOf.EM_Mastitis == null || Pawn == null || !Pawn.RaceProps.Humanlike || !Pawn.IsLactating()) return;
        if (!EqualMilkingSettings.allowMastitis) return;
        bool longFull = ticksFullPool >= 60000;
        float hygieneRisk = DubsBadHygieneIntegration.GetHygieneRiskFactorForMastitis(Pawn);
        bool badHygiene = hygieneRisk >= 0.4f;
        bool torsoInjury = HasTorsoOrBreastInjury(Pawn);
        if (!longFull && !badHygiene && !torsoInjury) return;
        float mtbDays = EqualMilkingSettings.mastitisBaseMtbDays;
        if (mtbDays < 0.1f) mtbDays = 0.1f;
        if (longFull) mtbDays /= Mathf.Max(0.1f, EqualMilkingSettings.overFullnessRiskMultiplier);
        if (badHygiene) mtbDays /= Mathf.Max(0.1f, (0.5f + hygieneRisk) * EqualMilkingSettings.hygieneRiskMultiplier);
        if (torsoInjury) mtbDays /= 1.3f;
        // 营养/饥饿缓解：饱腹时 MTB 延长（风险降低），饥饿时缩短
        float nutritionFactor = 0.5f + 0.5f * Mathf.Clamp(PawnUtility.BodyResourceGrowthSpeed(Pawn), 0f, 1f);
        mtbDays *= Mathf.Max(0.3f, nutritionFactor);
        float raceMultiplier = Pawn.RaceProps.Humanlike ? EqualMilkingSettings.mastitisMtbDaysMultiplierHumanlike : EqualMilkingSettings.mastitisMtbDaysMultiplierAnimal;
        mtbDays *= Mathf.Max(0.01f, raceMultiplier);
        if (!Rand.MTBEventOccurs(mtbDays, 60000f, 2000f)) return;
        var existing = Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_Mastitis);
        if (existing != null)
        {
            if (existing.Severity < 0.99f)
                existing.Severity = Mathf.Min(1f, existing.Severity + 0.15f);
        }
        else
            Pawn.health.AddHediff(EMDefOf.EM_Mastitis, Pawn.GetBreastOrChestPart());
    }

    private static bool HasTorsoOrBreastInjury(Pawn pawn)
    {
        if (pawn?.health?.hediffSet?.hediffs == null) return false;
        foreach (var h in pawn.health.hediffSet.hediffs)
        {
            if (h.Part?.def?.defName == null) continue;
            string dn = h.Part.def.defName;
            if (dn.Contains("Torso") || dn.Contains("Breast") || dn.Contains("Chest")) return true;
        }
        return false;
    }
    /// <summary>是否允许被该泌乳者自动喂食。仅看 canBeFed 开关；不再使用「谁可以喂我」列表。</summary>
    public bool AllowedToBeAutoFedBy(Pawn pawn)
    {
        return this.MilkSettings?.canBeFed == true;
    }
}
