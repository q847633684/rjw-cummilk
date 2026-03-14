using System.Collections.Generic;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Lactation.Jobs;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Comps;

/// <summary>平等挤奶 Comp：池存储与同步见 PoolStorage；进水/溢出/回缩见 Inflow；扣量见 Drain；流速与产奶见 Milking。见 Docs/泌乳系统逻辑图。</summary>
public partial class CompEquallyMilkable : CompMilkable
{
    protected Pawn Pawn => parent as Pawn;
    /// <summary>产出周期已移除，挤奶间隔固定为 1 天（水池模型不依赖此值）</summary>
    protected override int GatherResourcesIntervalDays => 1;
    protected override int ResourceAmount => (int)Pawn.MilkAmount();
    protected override ThingDef ResourceDef => Pawn.MilkDef();
    protected virtual float fResourceAmount => Pawn.MilkAmount();
    /// <summary>吸奶 session 累计量（池单位，1 池 = 1 瓶）。吸奶统一到 Drain 后由 ChildcareHelper 累加，结束时用于 amountFed/心情/outcomeDoers。</summary>
    public float breastfedAmount = 0f;
    public float maxFullness = 1f;

    /// <summary>水池模型：左乳水位（0～左乳容量），与右乳合计 0～maxFullness；总容量 = 左+右（正常为 1+1=2）。由 SyncLeftRightFromBreastFullness 从 breastFullness 汇总</summary>
    private float leftFullness;
    /// <summary>水池模型：右乳水位（0～右乳基础容量），与左乳合计 0～maxFullness</summary>
    private float rightFullness;
    /// <summary>按单个 key（Part.def.defName 或 defName_L/R）的水位，用于左1/右1/左2/右2 独立进水与展示。有则优先；无则由 left/right 反推</summary>
    private Dictionary<string, float> breastFullness = new Dictionary<string, float>();

    /// <summary>当前总奶量（0~maxFullness），双池和 = leftFullness + rightFullness；对外只读</summary>
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
    /// <summary>谁可以使用我的奶。名单为空时会预填子女/伴侣；仅名单内的人可吸奶/挤奶</summary>
    internal List<Pawn> allowedSucklers = new();
    /// <summary>谁可以使用我产出的奶/精液制品（不含自己，自己始终允许）。空列表 = 仅自己；非空 = 自己+列表中人。囚犯/奴隶产主时亦不默认允许殖民者（7.4）</summary>
    internal List<Pawn> allowedConsumers = new();
    private int updateTick = 0;
    private bool cachedActive = false;
    /// <summary>满池溢出：累计溢出量，达到阈值时生成地面污物（不扣水位）</summary>
    private float overflowAccumulator = 0f;
    /// <summary>10.8-6：最近一次被挤奶的游戏 tick，用于「长时间未挤奶」心情判定</summary>
    private int lastGatheredTick = -1;
    /// <summary>规格：连续满池（Fullness ≥ 5% maxFullness）的 tick 数，用于乳腺炎堵塞触发</summary>
    private int ticksFullPool;
    /// <summary>3.3 满池事件：上次发送「需要挤奶」信件的 tick，避免刷屏</summary>
    private int lastFullPoolLetterTick = -1;
    /// <summary>四层模型（阶段 0.2）：组织适应导致的容量增量，maxFullness = 基础容量 + 本值</summary>
    private float capacityAdaptation;
    /// <summary>2.3：药物泌乳状态缓存；仅状态变化时增删 EM_LactatingGain，检查间隔 60 tick</summary>
    private bool cachedWasLactatingWithDrugInduced = false;
    /// <summary>满池溢出累计量（仅 Debug 显示用）</summary>
    internal float OverflowAccumulator => overflowAccumulator;
    /// <summary>上一轮 UpdateMilkPools 是否执行过向基础容量的回缩；界面仅在有回缩时显示回缩吸收。</summary>
    private bool hadShrinkLastStep = false;
    /// <summary>按池：该侧是否已触发溢出逻辑（本步或之前溢出且尚未回缩到基础容量）；为 true 时该侧停止泌乳进水并每 60 tick 回缩，直到该侧满度≤基础容量后清除。</summary>
    private Dictionary<string, bool> overflowTriggeredByKey = new Dictionary<string, bool>();

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref breastfedAmount, "BreastfedAmount", 0f);
        Scribe_Values.Look(ref leftFullness, "PoolLeftFullness", 0f);
        Scribe_Values.Look(ref rightFullness, "PoolRightFullness", 0f);
        Scribe_Values.Look(ref overflowAccumulator, "PoolOverflowAccumulator", 0f);
        Scribe_Values.Look(ref lastGatheredTick, "PoolLastGatheredTick", -1);
        Scribe_Values.Look(ref ticksFullPool, "PoolTicksFull", 0);
        Scribe_Values.Look(ref lastFullPoolLetterTick, "PoolLastFullPoolLetterTick", -1);
        Scribe_Values.Look(ref capacityAdaptation, "EM.CapacityAdaptation", 0f);
        List<string> breastKeys = (Scribe.mode == LoadSaveMode.Saving && breastFullness != null)
            ? new List<string>(breastFullness.Keys)
            : new List<string>();
        List<float> breastVals = (Scribe.mode == LoadSaveMode.Saving && breastFullness != null)
            ? new List<float>(breastFullness.Values)
            : new List<float>();
        Scribe_Collections.Look(ref breastKeys, "BreastFullnessKeys", LookMode.Value);
        Scribe_Collections.Look(ref breastVals, "BreastFullnessValues", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            breastFullness ??= new Dictionary<string, float>();
            breastFullness.Clear();
            if (breastKeys != null && breastVals != null && breastKeys.Count == breastVals.Count)
            {
                for (int i = 0; i < breastKeys.Count; i++)
                    if (!string.IsNullOrEmpty(breastKeys[i]))
                        breastFullness[breastKeys[i]] = breastVals[i];
            }
            SyncLeftRightFromBreastFullness();
        }
        Scribe_Deep.Look(ref milkSettings, "MilkSettings");
        Scribe_Collections.Look(ref allowedSucklers, "AllowedSucklers", LookMode.Reference);
        Scribe_Collections.Look(ref allowedConsumers, "AllowedConsumers", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            EnsureSaveCompatAllowedLists();
            if (lastGatheredTick < 0)
                lastGatheredTick = Find.TickManager.TicksGame;
        }
        SyncBaseFullness();
    }

    /// <summary>10.8-6：最近一次被挤奶的游戏 tick；供 ThoughtWorker_LongTimeNotMilked 使用</summary>
    public int LastGatheredTick => lastGatheredTick;

    /// <summary>读档时调用（PostLoadInit）：确保列表非 null、移除无效引用；名单为空时预填子女/伴侣（仅同地图）。</summary>
    public void EnsureSaveCompatAllowedLists()
    {
        allowedSucklers ??= new List<Pawn>();
        allowedConsumers ??= new List<Pawn>();
        allowedSucklers.RemoveAll(p => p == null || p.Destroyed);
        allowedConsumers.RemoveAll(p => p == null || p.Destroyed);
        if (allowedSucklers.Count == 0 && parent is Pawn p)
        {
            var defaults = MilkPermissionExtensions.GetDefaultSucklers(p);
            foreach (Pawn pawn in defaults)
            {
                if (pawn == null || pawn.Destroyed || allowedSucklers.Contains(pawn)) continue;
                if (p.MapHeld != null && pawn.MapHeld != null && pawn.MapHeld != p.MapHeld) continue;
                allowedSucklers.Add(pawn);
            }
        }
    }

    /// <summary>仅做纯判断，不修改健康系统。无乳房（无乳池）时不执行泌乳逻辑，见 记忆库 design/泌乳前提-仅在有乳房时。Hediff 的增删在 CompTick 的 EnsureLactatingHediffFromConditions 中执行</summary>
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
            cachedActive = pawn.IsLactating() && pawn.IsMilkable() && (pawn == Pawn ? GetCachedEntries().Count : pawn.GetBreastPoolEntries().Count) > 0;
            updateTick = Find.TickManager.TicksGame + 500;
            return cachedActive;
        }
    }

    public override void CompTick()
    {
        if (!parent.IsHashIntervalTick(60)) { return; }
        // 每 tick 同步总容量（单乳 0.5 / 双乳 (左+右Severity)/2），供满池判定与显示
        if (parent is Pawn p)
        {
            float baseMax = Mathf.Max(0.01f, p.GetLeftBreastCapacityFactor() + p.GetRightBreastCapacityFactor());
            if (MilkCumSettings.enableTissueAdaptation)
            {
                float effectiveMax = baseMax + capacityAdaptation;
                float P = effectiveMax > 0f ? Mathf.Clamp01(Fullness / effectiveMax) : 0f;
                float step = 60f / 60000f; // 每 60 tick = 0.001 游戏日
                float theta = Mathf.Max(0f, MilkCumSettings.adaptationTheta);
                float omega = Mathf.Max(0f, MilkCumSettings.adaptationOmega);
                capacityAdaptation += step * (theta * Mathf.Max(P - 0.85f, 0f) - omega * (1f - P));
                float capMax = baseMax * Mathf.Clamp(MilkCumSettings.adaptationCapMaxRatio, 0f, 1f);
                capacityAdaptation = Mathf.Clamp(capacityAdaptation, 0f, capMax);
            }
            maxFullness = baseMax + capacityAdaptation;
            // 池子上限统一为撑大总容量（开/关压力因子都在此处停产、允许填到撑大、溢出）
            var entriesForCap = GetCachedEntries();
            float stretchTotal = maxFullness;
            if (entriesForCap.Count > 0)
            {
                stretchTotal = 0f;
                for (int i = 0; i < entriesForCap.Count; i++)
                    stretchTotal += entriesForCap[i].Capacity * PoolModelConstants.StretchCapFactor;
            }
            if (leftFullness + rightFullness > stretchTotal)
                SetFullness(stretchTotal, stretchTotal);
        }
        // 基因/物种/设置驱动的 Lactating 维护、药物泌乳增益、胀满 hediff 变化很慢，20 tick（约 1 秒）足够
        if (parent.IsHashIntervalTick(120))
        {
            EnsureLactatingHediffFromConditions();
            ApplyDrugInducedLactationEffects();
            MilkRelatedHealthHelper.UpdateBreastsEngorged(Pawn, Fullness, maxFullness);
        }
        if (!Active) { return; }
        // LOD：非当前地图的 pawn 每 300 tick 更新一次池，降低规模大时的负担；需结合 Profiler 验证
        bool onCurrentMap = Pawn?.MapHeld == null || Pawn.MapHeld == Find.CurrentMap;
        if (onCurrentMap || parent.IsHashIntervalTick(300))
            UpdateMilkPools();
        if (parent.IsHashIntervalTick(2000)) MilkRelatedHealthHelper.TryTriggerMastitisFromMtb(Pawn, Fullness, ticksFullPool);
    }

    /// <summary>根据基因/物种/设置维护 Lactating Hediff（增删与 Severity），仅在 CompTick 调用，避免 Active getter 产生副作用</summary>
    private void EnsureLactatingHediffFromConditions()
    {
        if (parent is not Pawn pawn || !pawn.SpawnedOrAnyParentSpawned || !pawn.IsColonyPawn() || pawn.Faction == null)
            return;
        // 永久泌乳基因：确保有 Lactating 并维持高 severity
        if (pawn.genes?.HasActiveGene(MilkCumDefOf.EM_Permanent_Lactation) == true)
        {
            Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating, pawn.GetBreastOrChestPart());
            lactating.Severity = Mathf.Max(lactating.Severity, 0.9999f);
            MilkCumSettings.LactationLog($"Lactating ensured (permanent gene): {pawn.Name}");
            return;
        }
        // 非人形且不可挤奶：移除 Lactating
        if (!pawn.RaceProps.Humanlike && !pawn.IsMilkable())
        {
            if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating) is Hediff lactating)
            {
                MilkCumSettings.LactationLog($"Lactating removed (not milkable): {pawn.Name}");
                pawn.health.RemoveHediff(lactating);
            }
            return;
        }
        // 设置：殖民地成年雌性动物始终泌乳
        if (MilkCumSettings.femaleAnimalAdultAlwaysLactating && pawn.IsAdultFemaleAnimalOfColony())
        {
            Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating, pawn.GetBreastOrChestPart());
            lactating.Severity = Mathf.Max(lactating.Severity, 1f);
            MilkCumSettings.LactationLog($"Lactating ensured (animal always): {pawn.Name}");
        }
    }

    /// <summary>药物诱发泌乳时仅添加泌乳增益 Hediff。每 120 tick 检查一次，状态变化时才增删（有缓存，减少重复查找）</summary>
    private void ApplyDrugInducedLactationEffects()
    {
        if (Pawn == null || !Pawn.RaceProps.Humanlike) return;
        bool nowLactatingWithDrug = Pawn.IsLactating() && Pawn.HasDrugInducedLactation();
        if (cachedWasLactatingWithDrugInduced == nowLactatingWithDrug)
            return;
        cachedWasLactatingWithDrugInduced = nowLactatingWithDrug;

        if (MilkCumDefOf.EM_LactatingGain != null)
        {
            if (nowLactatingWithDrug)
            {
                if (Pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_LactatingGain) == null)
                    Pawn.health.AddHediff(MilkCumDefOf.EM_LactatingGain, Pawn.GetBreastOrChestPart());
            }
            else if (Pawn.health.hediffSet.GetFirstHediffOfDef(MilkCumDefOf.EM_LactatingGain) is Hediff gain)
                Pawn.health.RemoveHediff(gain);
        }
    }

    /// <summary>是否有躯干/乳房损伤；委托给 MilkRelatedHealthHelper，供炎症模型等使用</summary>
    public static bool HasTorsoOrBreastInjury(Pawn pawn) => MilkRelatedHealthHelper.HasTorsoOrBreastInjury(pawn);
    /// <summary>是否允许被该泌乳者自动喂食。仅看 canBeFed 开关；不再使用「谁可以喂我」列表</summary>
    public bool AllowedToBeAutoFedBy(Pawn pawn)
    {
        return this.MilkSettings?.canBeFed == true;
    }
}
