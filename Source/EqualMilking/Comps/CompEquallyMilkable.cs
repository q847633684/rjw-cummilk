using Verse;
using RimWorld;
using UnityEngine;
using EqualMilking.Helpers;
using System.Collections.Generic;
using System.Linq;
using EqualMilking.Data;

namespace EqualMilking;
public class CompEquallyMilkable : CompMilkable
{
    protected Pawn Pawn => parent as Pawn;
    protected override int GatherResourcesIntervalDays => Mathf.Max((int)Props.milkIntervalDays, 1);
    protected float fGatherResourcesIntervalDays => Pawn.MilkIntervalDays();
    protected override int ResourceAmount => (int)Pawn.MilkAmount();
    protected override ThingDef ResourceDef => Pawn.MilkDef();
    protected virtual float fResourceAmount => Pawn.MilkAmount();
    protected virtual float GrowthMultiplier => Pawn.MilkGrowthMultiplier();
    public float breastfedAmount = 0f;
    public float maxFullness = 1f;

    /// <summary>水池模型：左乳水位（0～0.5），与右乳合计 0～1。</summary>
    private float leftFullness;
    /// <summary>水池模型：右乳水位（0～0.5），与左乳合计 0～1。</summary>
    private float rightFullness;
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
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref breastfedAmount, "BreastfedAmount", 0f);
        Scribe_Values.Look(ref leftFullness, "PoolLeftFullness", 0f);
        Scribe_Values.Look(ref rightFullness, "PoolRightFullness", 0f);
        Scribe_Deep.Look(ref milkSettings, "MilkSettings");
        Scribe_Collections.Look(ref assignedFeeders, "CanBeFedBy", LookMode.Reference);
        Scribe_Collections.Look(ref allowedSucklers, "AllowedSucklers", LookMode.Reference);
        Scribe_Collections.Look(ref allowedConsumers, "AllowedConsumers", LookMode.Reference);
        // 旧存档兼容：无双池时用 base.fullness 均分
        if (Scribe.mode == LoadSaveMode.PostLoadInit && leftFullness <= 0f && rightFullness <= 0f && fullness > 0f)
        {
            leftFullness = rightFullness = Mathf.Clamp(fullness * 0.5f, 0f, HalfPool);
        }
        SyncBaseFullness();
    }

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
        if (!Active) { return; }
        // 水池模型：进水流速 = 当前泌乳量×饥饿系数，两侧各得一半
        var lactatingComp = Pawn?.LactatingHediffWithComps()?.comps?.OfType<HediffComp_EqualMilkingLactating>().FirstOrDefault();
        if (lactatingComp == null || lactatingComp.RemainingDays <= 0f) { return; }
        float currentLactation = lactatingComp.CurrentLactationAmount;
        float hungerFactor = PawnUtility.BodyResourceGrowthSpeed(Pawn);
        if (currentLactation <= 0f || hungerFactor <= 0f) { return; }
        float flowPerDay = currentLactation * hungerFactor;
        float addPerTickTotal = flowPerDay / 60000f * 30f;
        float addEach = addPerTickTotal * 0.5f;
        leftFullness = Mathf.Min(HalfPool, leftFullness + addEach);
        rightFullness = Mathf.Min(HalfPool, rightFullness + addEach);
        SyncBaseFullness();
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
        // 7.5：被强制挤奶时给产主负面记忆
        if (parent is Pawn producer && producer.RaceProps.Humanlike && producer.needs?.mood?.thoughts?.memories != null
            && !ExtensionHelper.IsAllowedSuckler(producer, doer))
        {
            producer.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_ForcedMilking);
        }
    }
    /// <summary>是否允许被该泌乳者自动喂食。仅看 canBeFed 开关；不再使用「谁可以喂我」列表。</summary>
    public bool AllowedToBeAutoFedBy(Pawn pawn)
    {
        return this.MilkSettings?.canBeFed == true;
    }
}
