using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Lactation.Jobs;
using MilkCum.Core.Constants;
using MilkCum.Fluids.Shared.Data;
using MilkCum.RJW;
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
    // 统一数据源：vanilla 的 ResourceAmount 也改为使用乳池单位（Fullness），避免 milkAmount 与实际产出/扣量产生歧义。
    protected override int ResourceAmount => (int)Fullness;
    protected override ThingDef ResourceDef => Pawn.MilkDef();
    // 对应的浮点资源量同样以乳池单位表示。
    protected virtual float fResourceAmount => Fullness;
    /// <summary>吸奶 session 累计量（池单位，1 池 = 1 瓶）。吸奶统一到 Drain 后由 ChildcareHelper 累加，结束时用于 amountFed/心情/outcomeDoers。</summary>
    public float breastfedAmount = 0f;
    public float maxFullness = 1f;
    /// <summary>缓存基础容量（GetLeft+GetRight），每 600 tick 刷新，减少每 60 tick 重复计算。</summary>
    private float cachedBaseMaxFullness = 1f;
    private int lastBaseCapacityTick = -1;
    private const int BaseCapacityCacheInterval = 600;

    /// <summary>虚拟乳槽键（BreastLeft/BreastRight）→ 水位；Fullness = Sum(Values)。</summary>
    private Dictionary<string, float> breastFullness = new Dictionary<string, float>();

    /// <summary>当前总奶量（0~maxFullness），= breastFullness.Values.Sum()；对外只读。</summary>
    public new float Fullness
    {
        get
        {
            if (breastFullness == null || breastFullness.Count == 0) return 0f;
            float sum = 0f;
            foreach (var v in breastFullness.Values) sum += v;
            return sum;
        }
    }

    /// <summary>虚拟左槽（<see cref="FluidSiteKind.BreastLeft"/>）水位。</summary>
    public float LeftFullness => GetSideFullnessSum(isLeftSide: true);
    /// <summary>虚拟右槽（<see cref="FluidSiteKind.BreastRight"/>）水位。</summary>
    public float RightFullness => GetSideFullnessSum(isLeftSide: false);
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
    /// <summary>谁可以对我进行「吸奶/哺乳」（直接吸奶）。名单为空时会预填子女/伴侣。</summary>
    internal List<Pawn> allowedBreastfeeders = new();
    /// <summary>谁可以对我进行「挤奶」（使用挤奶器/机器挤奶）。名单为空时会预填子女/伴侣。</summary>
    internal List<Pawn> allowedMilkers = new();
    /// <summary>谁可以使用我产出的奶/精液制品（不含自己，自己始终允许）。空列表 = 仅自己；非空 = 自己+列表中人。囚犯/奴隶产主时亦不默认允许殖民者（7.4）</summary>
    internal List<Pawn> allowedConsumers = new();
    private int updateTick = 0;
    private bool cachedActive = false;
    /// <summary>满池溢出：累计溢出量，达到阈值时生成地面污物（不扣水位）</summary>
    private float overflowAccumulator = 0f;
    /// <summary>10.8-6：最近一次被挤奶的游戏 tick，用于「长时间未挤奶」心情判定</summary>
    private int lastGatheredTick = -1;
    /// <summary>各物理池 key 连续「达基础容量满阈」的累计 tick，用于瘀积/MTB/信件；与 breastFullness 同周期更新。</summary>
    private Dictionary<string, int> ticksFullPoolByKey = new Dictionary<string, int>();
    /// <summary>3.3 满池事件：上次发送「需要挤奶」信件的 tick，避免刷屏</summary>
    private int lastFullPoolLetterTick = -1;
    /// <summary>3.3 满池信件：上次发送时的游戏日，用于「每 Pawn 每日最多一封」限制</summary>
    private int lastFullPoolLetterDay = -1;
    /// <summary>双相组织适应：快速可逆分量（短期涨落）。</summary>
    private float capacityAdaptationFast;
    /// <summary>双相组织适应：慢速重塑分量（长期结构变化）。</summary>
    private float capacityRemodelSlow;
    /// <summary>事件驱动进水子步长突发剩余 tick；&gt;0 时使用高频子步推进。</summary>
    private int inflowEventBurstTicksRemaining;
    /// <summary>2.3：药物泌乳状态缓存；仅状态变化时增删 EM_LactatingGain，检查间隔 60 tick</summary>
    private bool cachedWasLactatingWithDrugInduced = false;
    /// <summary>满池溢出累计量（仅 Debug 显示用）</summary>
    internal float OverflowAccumulator => overflowAccumulator;
    /// <summary>上一轮 UpdateMilkPools 是否执行过向基础容量的回缩；界面仅在有回缩时显示回缩吸收。</summary>
    private bool hadShrinkLastStep = false;
    /// <summary>上一轮回缩时折算的「每日回缩吸收营养」，供 GetReabsorbedNutritionPerDay 直接返回，避免 UI/Needs 读取时再次遍历 entries。</summary>
    private float cachedReabsorbedNutritionPerDay = 0f;
    /// <summary>用于 PoolTickLog：记录每乳池侧本步前的满度，避免每次 UpdateMilkPools 分配新字典。</summary>
    private readonly Dictionary<string, float> fullnessBeforePerKeyCache = new Dictionary<string, float>();
    /// <summary>用于 PoolTickLog：记录每乳池侧本步回缩量，避免每次 UpdateMilkPools 分配新字典。</summary>
    private readonly Dictionary<string, float> reabsorbedPerKeyCache = new Dictionary<string, float>();
    /// <summary>统计：累计产奶量（池单位），用于成就/UI</summary>
    private float totalDrainedLifetime;
    /// <summary>统计：被挤奶/吸奶次数（每次 SpawnBottles 或等效产出计 1）</summary>
    private int gatherCountLifetime;
    /// <summary>统计：满池溢出触发污物次数</summary>
    private int overflowEventCount;

    /// <summary>池逻辑在 UpdateMilkPools 中写入：本步实际进水流速（池单位/天），供 UI 直接读取，与回缩/溢出等逻辑一致。不存档。</summary>
    internal float CachedFlowPerDayForDisplay;
    /// <summary>写入 CachedFlowPerDayForDisplay 时的游戏 tick，用于判断缓存是否有效（60 tick 内有效）。</summary>
    internal int CachedFlowTick = -1;
    /// <summary>池逻辑在 UpdateMilkPools 中按 key 写入的每乳池侧流速（池单位/天），供 GetFlowPerDayForBreastSides 等直接读取，不存档。</summary>
    internal Dictionary<string, float> CachedFlowPerDayByKey;
    /// <summary>池逻辑中按实际进水流速加权的压力/喷乳反射/状态因子，供 GetFlowPerDayBreakdown 悬停显示，不存档。</summary>
    internal float CachedPressureForDisplay, CachedLetdownForDisplay, CachedConditionsForDisplay;
    /// <summary>每 60 tick 在 CompTick 中刷新，供 IsCachedFlowValid 使用，避免每次调用 Find.TickManager.TicksGame。</summary>
    private int cachedTicksGameForFlow = -1;

    /// <summary>缓存是否在 60 tick 内有效。</summary>
    internal bool IsCachedFlowValid() => CachedFlowTick >= 0 && cachedTicksGameForFlow >= 0 && (cachedTicksGameForFlow - CachedFlowTick) <= 60;

    /// <summary>池基础总容量（按 entries 累加），供 RJW 撑大/回缩同步用；无条目时退回 maxFullness - CapacityAdaptation。</summary>
    internal float GetPoolBaseTotal()
    {
        var entries = GetCachedEntries();
        if (entries.Count == 0) return Mathf.Max(0.01f, maxFullness - CapacityAdaptation);
        float sum = 0f;
        for (int i = 0; i < entries.Count; i++) sum += entries[i].Capacity;
        return Mathf.Max(0.01f, sum);
    }
    /// <summary>池撑大总容量（= 基础×StretchCapFactor），供 RJW 撑大/回缩同步用。</summary>
    internal float GetPoolStretchTotal() => GetPoolBaseTotal() * PoolModelConstants.StretchCapFactor;
    /// <summary>池基础总容量（含组织适应摊入各乳池侧后的 entries 之和），与 maxFullness 一致；供 UI/外部读取。</summary>
    public float GetPoolBaseCapacityTotal() => GetPoolBaseTotal();
    /// <summary>物理撑大总容量（池内可蓄满上限）；UI 满度分母、乳腺炎比例等应优先用本值而非仅 maxFullness。</summary>
    public float GetPoolStretchCapacityTotal() => GetPoolStretchTotal();
    /// <summary>组织适应总增量（快适应+慢重塑）；由 GetBreastPoolEntries 按比例摊入各 FluidPoolEntry.Capacity。</summary>
    internal float CapacityAdaptation => capacityAdaptationFast + capacityRemodelSlow;
    /// <summary>取指定 key 的缓存流速（池单位/天），无缓存或 key 不存在则返回 0。</summary>
    internal float GetCachedFlowPerDayForKey(string key) => CachedFlowPerDayByKey != null && CachedFlowPerDayByKey.TryGetValue(key, out float v) ? v : 0f;
    /// <summary>获取缓存的总流速（池单位/天），缓存无效时返回 0。</summary>
    internal float GetTotalFlowPerDayCached() => IsCachedFlowValid() ? CachedFlowPerDayForDisplay : 0f;
    /// <summary>获取缓存的指定 key 流速（池单位/天），若缓存失效则返回 0。</summary>
    internal float GetFlowPerDayForKeyCached(string key) => IsCachedFlowValid() ? GetCachedFlowPerDayForKey(key) : 0f;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref breastfedAmount, "BreastfedAmount", 0f);
        Scribe_Values.Look(ref overflowAccumulator, "PoolOverflowAccumulator", 0f);
        Scribe_Values.Look(ref lastGatheredTick, "PoolLastGatheredTick", -1);
        List<string> ticksFullKeys = null;
        List<int> ticksFullVals = null;
        if (Scribe.mode == LoadSaveMode.Saving && ticksFullPoolByKey != null && ticksFullPoolByKey.Count > 0)
        {
            ticksFullKeys = ticksFullPoolByKey.Keys.ToList();
            ticksFullVals = ticksFullPoolByKey.Values.ToList();
        }
        Scribe_Collections.Look(ref ticksFullKeys, "EM.PoolTicksFullSiteKeys", LookMode.Value);
        Scribe_Collections.Look(ref ticksFullVals, "EM.PoolTicksFullSiteVals", LookMode.Value);
        Scribe_Values.Look(ref lastFullPoolLetterTick, "PoolLastFullPoolLetterTick", -1);
        Scribe_Values.Look(ref lastFullPoolLetterDay, "EM.LastFullPoolLetterDay", -1);
        Scribe_Values.Look(ref capacityAdaptationFast, "EM.CapacityAdaptationFast", 0f);
        Scribe_Values.Look(ref capacityRemodelSlow, "EM.CapacityRemodelSlow", 0f);
        Scribe_Values.Look(ref totalDrainedLifetime, "EM.TotalDrainedLifetime", 0f);
        Scribe_Values.Look(ref gatherCountLifetime, "EM.GatherCountLifetime", 0);
        Scribe_Values.Look(ref overflowEventCount, "EM.OverflowEventCount", 0);
        Scribe_Collections.Look(ref breastFullness, "EM.FluidSitesMilk", LookMode.Value, LookMode.Value);
        if (Scribe.mode != LoadSaveMode.Saving)
            breastFullness ??= new Dictionary<string, float>();
        Scribe_Deep.Look(ref milkSettings, "MilkSettings");
        Scribe_Collections.Look(ref allowedBreastfeeders, "AllowedBreastfeeders", LookMode.Reference);
        Scribe_Collections.Look(ref allowedMilkers, "AllowedMilkers", LookMode.Reference);
        Scribe_Collections.Look(ref allowedConsumers, "AllowedConsumers", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            ticksFullPoolByKey ??= new Dictionary<string, int>();
            ticksFullPoolByKey.Clear();
            if (ticksFullKeys != null && ticksFullVals != null && ticksFullKeys.Count == ticksFullVals.Count)
            {
                for (int i = 0; i < ticksFullKeys.Count; i++)
                    if (!string.IsNullOrEmpty(ticksFullKeys[i]))
                        ticksFullPoolByKey[ticksFullKeys[i]] = Mathf.Max(0, ticksFullVals[i]);
            }
            EnsureAllowedLists();
            if (milkSettings == null && parent is Pawn pawn)
                milkSettings = pawn.GetDefaultMilkSetting();
            if (lastGatheredTick < 0)
                lastGatheredTick = Find.TickManager.TicksGame;
        }
        SyncBaseFullness();
    }

    /// <summary>10.8-6：最近一次被挤奶的游戏 tick；供 ThoughtWorker_LongTimeNotMilked 使用</summary>
    public int LastGatheredTick => lastGatheredTick;

    /// <summary>读档时调用（PostLoadInit）：确保列表非 null、移除无效引用；名单为空时预填子女/伴侣（仅同地图）。</summary>
    public void EnsureAllowedLists()
    {
        allowedBreastfeeders ??= new List<Pawn>();
        allowedMilkers ??= new List<Pawn>();
        allowedConsumers ??= new List<Pawn>();
        RemoveDestroyedFromAllowedList(allowedBreastfeeders);
        RemoveDestroyedFromAllowedList(allowedMilkers);
        RemoveDestroyedFromAllowedList(allowedConsumers);
        if (parent is Pawn p)
        {
            TryFillAllowedListFromDefaults(p, allowedBreastfeeders);
            TryFillAllowedListFromDefaults(p, allowedMilkers);
        }
    }

    private static void RemoveDestroyedFromAllowedList(List<Pawn> list)
    {
        if (list == null) return;
        list.RemoveAll(p => p == null || p.Destroyed);
    }

    /// <summary>名单为空时从默认吸奶对象预填（同地图）。</summary>
    private static void TryFillAllowedListFromDefaults(Pawn owner, List<Pawn> list)
    {
        if (list.Count != 0) return;
        foreach (Pawn pawn in MilkPermissionExtensions.GetDefaultSucklers(owner))
        {
            if (pawn == null || pawn.Destroyed || list.Contains(pawn)) continue;
            if (owner.MapHeld != null && pawn.MapHeld != null && pawn.MapHeld != owner.MapHeld) continue;
            list.Add(pawn);
        }
    }

    /// <summary>仅做纯判断，不修改健康系统。无乳房（无乳池）时不执行泌乳逻辑。Hediff 的增删在 CompTick 的 EnsureLactatingHediffFromConditions 中执行。getter 无副作用，仅返回最近一次 RefreshActive 的结果。</summary>
    protected override bool Active => cachedActive;

    /// <summary>显式刷新并返回当前是否处于可泌乳活动状态；CompTick 每 60 tick 调用，避免 getter 产生隐式状态变更。</summary>
    internal bool IsActiveNow()
    {
        if (parent.Faction == null || parent is not Pawn pawn || !parent.SpawnedOrAnyParentSpawned || !pawn.IsColonyPawn())
        {
            cachedActive = false;
            return false;
        }
        int entryCount = pawn == Pawn
            ? GetCachedEntries().Count
            : (pawn.CompEquallyMilkable()?.GetCachedEntriesIfValid() ?? pawn.GetBreastPoolEntries()).Count;
        cachedActive = pawn.IsLactating() && pawn.IsMilkable() && entryCount > 0;
        return cachedActive;
    }

    public override void CompTick()
    {
        if (!parent.IsHashIntervalTick(60)) { return; }
        int now = Find.TickManager.TicksGame;
        cachedTicksGameForFlow = now;
        updateTick = now + 120;
        IsActiveNow();
        if (parent is Pawn p)
        {
            if (cachedEntries == null || (now - lastBaseCapacityTick) > BaseCapacityCacheInterval)
            {
                cachedBaseMaxFullness = Mathf.Max(0.01f, p.GetLeftBreastCapacityFactor() + p.GetRightBreastCapacityFactor());
                lastBaseCapacityTick = now;
            }
            float baseMax = cachedBaseMaxFullness;
            if (MilkCumSettings.enableTissueAdaptation)
            {
                float effectiveMax = baseMax + CapacityAdaptation;
                float P = effectiveMax > 0f ? Mathf.Clamp01(Fullness / effectiveMax) : 0f;
                float realTicksPassed = 60f;
                float daysPassed = realTicksPassed / GenDate.TicksPerDay;
                float theta = Mathf.Max(0f, MilkCumSettings.adaptationTheta);
                float omega = Mathf.Max(0f, MilkCumSettings.adaptationOmega);
                float fastGrow = theta * Mathf.Max(P - 0.80f, 0f);
                float fastDecay = omega * (1f - P);
                float slowGrow = Mathf.Max(0f, MilkCumSettings.adaptationSlowTheta) * Mathf.Max(P - 0.90f, 0f);
                float slowDecay = Mathf.Max(0f, MilkCumSettings.adaptationSlowOmega) * Mathf.Max(0f, 0.95f - P);
                capacityAdaptationFast += daysPassed * (fastGrow - fastDecay);
                capacityRemodelSlow += daysPassed * (slowGrow - slowDecay);
                float capMax = baseMax * Mathf.Clamp(MilkCumSettings.adaptationCapMaxRatio, 0f, 1f);
                capacityAdaptationFast = Mathf.Clamp(capacityAdaptationFast, 0f, capMax);
                capacityRemodelSlow = Mathf.Clamp(capacityRemodelSlow, 0f, capMax);
            }
            maxFullness = baseMax + CapacityAdaptation;
            if (MilkCumSettings.enableTissueAdaptation)
                SetEntriesCacheDirty();
            // 池子上限统一为撑大总容量（开/关压力因子都在此处停产、允许填到撑大、溢出）
            var entriesForCap = GetCachedEntries();
            float stretchTotal = maxFullness;
            if (entriesForCap.Count > 0)
            {
                stretchTotal = 0f;
                for (int i = 0; i < entriesForCap.Count; i++)
                    stretchTotal += entriesForCap[i].Capacity * PoolModelConstants.StretchCapFactor;
            }
            if (Fullness > stretchTotal)
                SetFullness(stretchTotal, stretchTotal);
        }
        // 基因/物种/设置驱动的 Lactating 维护、EM_LactatingGain、胀满 hediff 变化很慢，20 tick（约 1 秒）足够
        if (parent.IsHashIntervalTick(120))
        {
            EnsureEntriesCacheDirtyIfBreastCountChanged();
            MilkRelatedHealthHelper.EnsureLactatingHediffFromConditions(Pawn);
            ApplyDrugInducedLactationEffects();
            var milkComp120 = Pawn.CompEquallyMilkable();
            MilkRelatedHealthHelper.UpdateBreastsEngorged(Pawn, milkComp120);
            MilkRelatedHealthHelper.UpdateLactationalMilkStasis(Pawn, milkComp120);
        }
        if (!cachedActive) { return; }
        // LOD：当前地图每 60 tick；非当前地图 300 tick；未载入地图（商队等）600 tick，降低多档负担
        bool onCurrentMap = Pawn?.MapHeld != null && Pawn.MapHeld == Find.CurrentMap;
        bool notOnAnyMap = Pawn?.MapHeld == null;
        int interval = onCurrentMap ? 60 : (notOnAnyMap ? PoolModelConstants.LODIntervalNotOnMapTicks : 300);
        if (onCurrentMap || parent.IsHashIntervalTick(interval))
        {
            UpdateMilkPools();
            if (inflowEventBurstTicksRemaining > 0)
                inflowEventBurstTicksRemaining = Mathf.Max(0, inflowEventBurstTicksRemaining - 60);
            if (MilkCumSettings.rjwBreastSizeEnabled && Pawn != null && Pawn.IsInLactatingState())
                RJWLactatingBreastSizeGameComponent.SyncRJWBreastSeverityFromPool(Pawn);
        }
        if (parent.IsHashIntervalTick(2000))
        {
            var milkComp2k = Pawn.CompEquallyMilkable();
            MilkRelatedHealthHelper.TryTriggerMastitisFromMtb(Pawn, milkComp2k);
            MilkRelatedHealthHelper.TryTriggerBreastAbscessFromMtb(Pawn, milkComp2k);
        }
    }

    /// <summary>由 GameComponent 每 2500 tick 集中调用，移除已销毁/空的 allowed 列表引用。</summary>
    internal void CleanupAllowedLists()
    {
        RemoveDestroyedFromAllowedList(allowedBreastfeeders);
        RemoveDestroyedFromAllowedList(allowedMilkers);
        RemoveDestroyedFromAllowedList(allowedConsumers);
    }

    /// <summary>药物诱发泌乳时仅添加 EM_LactatingGain。每 120 tick 检查一次，状态变化时才增删（有缓存，减少重复查找）</summary>
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
    /// <summary>是否允许被该泌乳者自动喂食。仅看 canBeFed 开关；不再使用「谁可以喂我」列表（参数保留供调用方语义，未参与判定）。</summary>
    public bool AllowedToBeAutoFedBy(Pawn _) => MilkSettings?.canBeFed == true;

    /// <summary>挤奶/吸奶扣池后按侧降低炎症 I。</summary>
    public void NotifyInflammationDrain(Dictionary<string, float> drainedByKey)
    {
        if (drainedByKey == null || drainedByKey.Count == 0) return;
        Pawn?.LactatingHediffComp()?.ApplyDrainInflammationRelief(drainedByKey, this);
    }

    /// <summary>触发事件驱动进水子步长突发窗口（吸奶/挤奶/高潮等短时刺激后）。</summary>
    internal void TriggerInflowEventBurst()
    {
        int dur = Mathf.Max(0, MilkCumSettings.inflowEventBurstDurationTicks);
        if (dur <= 0) return;
        inflowEventBurstTicksRemaining = Mathf.Max(inflowEventBurstTicksRemaining, dur);
    }

    /// <summary>当前是否处于事件驱动进水子步长突发窗口。</summary>
    internal bool IsInflowEventBurstActive() => inflowEventBurstTicksRemaining > 0;
}
