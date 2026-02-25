using Verse;
using RimWorld;
using UnityEngine;
using EqualMilking.Helpers;
using System.Collections.Generic;
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
    /// <summary>当前奶量 fullness（0~maxFullness），对外只读，用于调度 Job 优先级等。</summary>
    public new float Fullness => fullness;
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
        Scribe_Deep.Look(ref milkSettings, "MilkSettings");
        Scribe_Collections.Look(ref assignedFeeders, "CanBeFedBy", LookMode.Reference);
        Scribe_Collections.Look(ref allowedSucklers, "AllowedSucklers", LookMode.Reference);
        Scribe_Collections.Look(ref allowedConsumers, "AllowedConsumers", LookMode.Reference);
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
        if (!Active || fullness == maxFullness) { return; }
        float num = maxFullness / (this.fGatherResourcesIntervalDays * 2000f); // 60000Ticks/day over 30 ticks/process
        fullness += num * GrowthMultiplier;
        if (fullness > maxFullness)
        {
            fullness = maxFullness;
        }
    }
    /// <summary>
    /// Set the fullness of the milkable pawn, will clamp the value between 0 and maxFullness
    /// </summary>
    /// <param name="value">new fullness value</param>
    public void SetFullness(float value)
    {
        fullness = Mathf.Clamp(value, 0f, maxFullness);
    }
    /// <summary>
    /// Gather the milk from the milkable pawn, place the milk in the milking spot or the doer's position. Overrides vanilla method.
    /// </summary>
    /// <param name="doer">the pawn that is gathering the milk</param>
    new public void Gathered(Pawn doer)
    {
        if (!Active)
        {
            Log.Error(string.Concat(doer, " gathered body resources while not Active: ", parent));
        }
        float yieldFactor = doer.GetStatValue(StatDefOf.AnimalGatherYield);
        Building_Milking milkingSpot = (doer.jobs?.curDriver as JobDriver_EquallyMilk)?.MilkBuilding;
        if (milkingSpot != null)
        {
            yieldFactor += milkingSpot.YieldOffset();
        }
        if (!Rand.Chance(yieldFactor) && parent.Map != null)
        {
            MoteMaker.ThrowText((doer.DrawPos + parent.DrawPos) / 2f, parent.Map, Lang.ProductWasted, 3.65f);
        }
        else
        {
            int num = GenMath.RoundRandom(fResourceAmount * fullness);
            Pawn pawn = parent as Pawn;
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
                {
                    milkingSpot.PlaceMilkThing(thing);
                }
                else
                {
                    GenPlace.TryPlaceThing(thing, doer.Position, doer.Map, ThingPlaceMode.Near);
                }
            }
        }
        fullness = 0f;
        // 7.5：被强制挤奶时给产主负面记忆（挤奶者不在允许吸奶名单内）
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
