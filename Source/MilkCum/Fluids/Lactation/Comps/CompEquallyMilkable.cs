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
public class CompEquallyMilkable : CompMilkable
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

    /// <summary>该侧是否处于溢出状态（已触发溢出且当前高于基础容量），用于停泌乳与回缩判定。</summary>
    private bool IsOverflowState(string key, float cur, float baseCap)
        => overflowTriggeredByKey.TryGetValue(key, out bool ov) && ov && cur > baseCap;

    /// <summary>按 key 取该乳当前水位，用于健康页悬停等；无该 key 时返回 0</summary>
    public float GetFullnessForKey(string key)
    {
        if (string.IsNullOrEmpty(key) || breastFullness == null) return 0f;
        return breastFullness.TryGetValue(key, out float v) ? v : 0f;
    }

    /// <summary>缓存 GetBreastPoolEntries()，每 60 tick 失效，减少每 tick 分配与 GC。</summary>
    private List<FluidPoolEntry> cachedEntries;
    private int cachedEntriesTick = -1;
    private const int CacheInvalidateInterval = 60;

    private List<FluidPoolEntry> GetCachedEntries()
    {
        int now = Find.TickManager.TicksGame;
        if (cachedEntries != null && (now - cachedEntriesTick) <= CacheInvalidateInterval)
            return cachedEntries;
        cachedEntries = Pawn != null ? Pawn.GetBreastPoolEntries() : new List<FluidPoolEntry>();
        cachedEntriesTick = now;
        return cachedEntries;
    }

    /// <summary>池侧数量（左+右等），用于机器挤奶并行扣量时算每侧速率。使用缓存，避免每 tick 分配。</summary>
    public int BreastSideCount => Mathf.Max(1, GetCachedEntries().Count);

    /// <summary>按 PairIndex 分组并按 PairIndex 顺序排列，用于 UpdateMilkPools。无 LINQ，减少 GC。</summary>
    private static List<List<FluidPoolEntry>> BuildPairGroupsByPairIndex(List<FluidPoolEntry> entries)
    {
        var pairIndexToGroup = new Dictionary<int, int>();
        var pairGroups = new List<List<FluidPoolEntry>>();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (!pairIndexToGroup.TryGetValue(e.PairIndex, out int idx))
            {
                idx = pairGroups.Count;
                pairIndexToGroup[e.PairIndex] = idx;
                pairGroups.Add(new List<FluidPoolEntry>());
            }
            pairGroups[idx].Add(e);
        }
        pairGroups.Sort((a, b) => (a.Count > 0 ? a[0].PairIndex : 0).CompareTo(b.Count > 0 ? b[0].PairIndex : 0));
        return pairGroups;
    }

    /// <summary>按 PairIndex 分组并按该对总满度降序排列，用于 Drain 选侧（最满的对先扣）。无 LINQ，减少 GC。</summary>
    private List<List<FluidPoolEntry>> BuildPairGroupsByFullnessDescending(List<FluidPoolEntry> entries)
    {
        var pairIndexToGroup = new Dictionary<int, int>();
        var pairGroups = new List<List<FluidPoolEntry>>();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (!pairIndexToGroup.TryGetValue(e.PairIndex, out int idx))
            {
                idx = pairGroups.Count;
                pairIndexToGroup[e.PairIndex] = idx;
                pairGroups.Add(new List<FluidPoolEntry>());
            }
            pairGroups[idx].Add(e);
        }
        float SumFullness(List<FluidPoolEntry> list)
        {
            float s = 0f;
            for (int j = 0; j < list.Count; j++)
                s += GetFullnessForKey(list[j].Key);
            return s;
        }
        pairGroups.Sort((a, b) => SumFullness(b).CompareTo(SumFullness(a)));
        return pairGroups;
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

    /// <summary>各侧的挤出流速（池单位/秒），与 GetCachedEntries() 顺序一致。手挤用其和；机器挤用每侧独立排空、并行，总耗时由最慢一侧决定。</summary>
    public List<float> GetMilkingFlowRatesPerSide(bool isMachine, Building_Milking building = null)
    {
        var list = new List<float>();
        float baseFlowPerSecond = 60f / Mathf.Max(0.01f, MilkCumSettings.milkingWorkTotalBase);
        float raceMult = MilkCumSettings.GetRaceDrugDeltaSMultiplier(Pawn);
        var lactatingComp = Pawn?.LactatingHediffComp();
        breastFullness ??= new Dictionary<string, float>();
        var entries = GetCachedEntries();
        float speedMult = 1f + (isMachine ? (building?.SpeedOffset() ?? 0f) : 0f);
        speedMult = Mathf.Max(0.01f, speedMult);
        if (lactatingComp == null)
        {
            for (int i = 0; i < entries.Count; i++)
                list.Add(baseFlowPerSecond * raceMult * speedMult);
            return list;
        }
        if (entries.Count == 0)
        {
            float letdown = Mathf.Max(0.01f, lactatingComp.GetLetdownReflexFlowMultiplier());
            float maxF = Mathf.Max(0.01f, maxFullness);
            float f = Mathf.Clamp01(Fullness / maxF);
            list.Add(baseFlowPerSecond * raceMult * (f * f) * letdown * speedMult);
            return list;
        }
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) { list.Add(0f); continue; }
            float fullness = GetFullnessForKey(e.Key);
            float cap = Mathf.Max(0.01f, e.Capacity);
            float f = Mathf.Clamp01(fullness / cap);
            float letdown = Mathf.Max(0.01f, lactatingComp.GetLetdownReflexFlowMultiplier(e.Key));
            list.Add(baseFlowPerSecond * raceMult * (f * f) * letdown * speedMult);
        }
        return list;
    }

    /// <summary>吸奶专用：按「即将被吸的那一侧」的流速，选侧规则与 DrainForConsumeSingleSide 一致；复用 GetMilkingFlowRatesPerSide 取该侧流速。</summary>
    public float GetMilkingFlowRateForSingleSide()
    {
        var rates = GetMilkingFlowRatesPerSide(false, null);
        if (rates.Count == 0) return 0f;
        int idx = GetFirstDrainSideIndex();
        return idx >= 0 && idx < rates.Count ? rates[idx] : rates[0];
    }

    /// <summary>吸奶/单侧扣量时「第一个会被扣」的侧在 GetCachedEntries() 中的下标；与 DrainForConsumeSingleSide 选侧一致。</summary>
    private int GetFirstDrainSideIndex()
    {
        var entries = GetCachedEntries();
        if (entries.Count == 0) return 0;
        var pairGroups = BuildPairGroupsByFullnessDescending(entries);
        string singleKey = null;
        for (int g = 0; g < pairGroups.Count; g++)
        {
            var list = pairGroups[g];
            if (list.Count == 2)
            {
                FluidPoolEntry leftE = list[0].IsLeft ? list[0] : list[1];
                FluidPoolEntry rightE = list[0].IsLeft ? list[1] : list[0];
                if (string.IsNullOrEmpty(leftE.Key) || string.IsNullOrEmpty(rightE.Key)) continue;
                float leftF = GetFullnessForKey(leftE.Key);
                float rightF = GetFullnessForKey(rightE.Key);
                bool drainLeftFirst = leftF > rightF || (Mathf.Approximately(leftF, rightF) && true);
                singleKey = drainLeftFirst ? leftE.Key : rightE.Key;
                break;
            }
            if (list.Count >= 1 && !string.IsNullOrEmpty(list[0].Key))
            {
                singleKey = list[0].Key;
                break;
            }
        }
        if (string.IsNullOrEmpty(singleKey)) return 0;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Key == singleKey) return i;
        return 0;
    }

    /// <summary>回缩吸收：仅当该侧触发了溢出逻辑且本步有回缩时，界面显示回缩吸收。本方法将回缩量折算为「每日补充营养」；无回缩时返回 0。</summary>
    public float GetReabsorbedNutritionPerDay()
    {
        if (!MilkCumSettings.reabsorbNutritionEnabled || Pawn == null || breastFullness == null) return 0f;
        if (!hadShrinkLastStep) return 0f;
        var entries = GetCachedEntries();
        if (entries.Count == 0) return 0f;
        float healthPercent = 1f;
        if (Pawn.health?.summaryHealth != null)
            healthPercent = Mathf.Clamp(Pawn.health.summaryHealth.SummaryHealthPercent, 0.2f, 1f);
        float shrinkFactor = (1f - PoolModelConstants.ShrinkPerStep) * healthPercent;
        float reabsorbedPoolPerStep = 0f;
        foreach (var e in entries)
        {
            if (!breastFullness.TryGetValue(e.Key, out float cur)) continue;
            if (!IsOverflowState(e.Key, cur, e.Capacity)) continue;
            reabsorbedPoolPerStep += (cur - e.Capacity) * (1f - shrinkFactor);
        }
        if (reabsorbedPoolPerStep <= 0f) return 0f;
        float reabsorbedPoolPerDay = reabsorbedPoolPerStep * (60000f / 60f); // 每 60 tick 一次
        return reabsorbedPoolPerDay * PoolModelConstants.NutritionPerPoolUnit
            * Mathf.Clamp01(MilkCumSettings.reabsorbNutritionEfficiency);
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

    /// <summary>将双池总和同步到基类 fullness，供可能读取基类字段的代码使用</summary>
    private void SyncBaseFullness()
    {
        fullness = Mathf.Clamp(leftFullness + rightFullness, 0f, maxFullness);
    }

    /// <summary>从 per-breast 字典汇总到 leftFullness / rightFullness（按 GetBreastPoolEntries 的 IsLeft）</summary>
    private void SyncLeftRightFromBreastFullness()
    {
        if (Pawn == null || breastFullness == null) return;
        var entries = GetCachedEntries();
        float left = 0f, right = 0f;
        foreach (var e in entries)
        {
            if (breastFullness.TryGetValue(e.Key, out float v))
            {
                if (e.IsLeft) left += v; else right += v;
            }
        }
        leftFullness = left;
        rightFullness = right;
    }

    /// <summary>泌乳结束时清空双池（由 HediffComp_EqualMilkingLactating 调用）</summary>
    public void ClearPools()
    {
        leftFullness = 0f;
        rightFullness = 0f;
        breastFullness?.Clear();
        SyncBaseFullness();
    }
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

    /// <summary>水池模型：按对进水；每对仅当两侧都达基础容量后才允许撑大（见 Docs/泌乳系统逻辑图）。撑大后仍按压力曲线进水，超出撑大部分算溢出（持续微量溢出）。仅当本步发生溢出时才执行池水位回缩与回缩吸收；回缩后满度降低、下一轮压力减小流速恢复，效果上从满池微量进水转为明显泌乳，形成逻辑闭环。回缩时超出基础容量部分每 60 tick 按 ShrinkPerStep 向基础容量收敛，回缩吸收由 GetReabsorbedNutritionPerDay 折算饱食度。</summary>
    private void UpdateMilkPools()
    {
        var lactatingComp = Pawn?.LactatingHediffComp();
        if (lactatingComp == null || lactatingComp.RemainingDays <= 0f) { return; }
        float currentLactation = lactatingComp.CurrentLactationAmount;
        float hungerFactor = PawnUtility.BodyResourceGrowthSpeed(Pawn);
        if (currentLactation <= 0f || hungerFactor <= 0f) { return; }
        float drive = MilkCumSettings.GetEffectiveDrive(currentLactation);
        float basePerDay = drive * hungerFactor
            * Pawn.GetMilkFlowMultiplierFromConditions()
            * Pawn.GetMilkFlowMultiplierFromGenes()
            * MilkCumSettings.defaultFlowMultiplierForHumanlike;
        var entries = GetCachedEntries();
        // Active 已保证有乳池才进入，此处不再判空
        breastFullness ??= new Dictionary<string, float>();
        float overflowTotal = 0f;
        float flowPerTickScale = basePerDay / 60000f * 60f;
        SyncLeftRightFromBreastFullness();
        // 先更新本 60 tick 内影响流速的状态，再扣营养与进水，保证 ExtraNutritionPerDay() 与进水循环用同一套 R/压力
        if (MilkCumSettings.enableLetdownReflex)
            lactatingComp.DecayLetdownReflex(60f / 60f); // Δt = 60 tick = 1 分钟
        MilkRelatedHealthHelper.UpdateInflammationAndTryTriggerMastitis(lactatingComp, Fullness, maxFullness);
        if (MilkCumSettings.enableToleranceDynamic)
            lactatingComp.UpdateToleranceDynamic(currentLactation, 60f / 60000f);
        float extraFall60 = 0f;
        if (Fullness < maxFullness && Pawn != null)
        {
            int basis = Mathf.Clamp(MilkCumSettings.lactationExtraNutritionBasis, 0, 300);
            float factor = basis / 150f;
            const float interval60PerDay = 60f / 60000f;
            extraFall60 = lactatingComp.ExtraNutritionPerDay() * factor * interval60PerDay;
            if (Pawn.needs?.food != null)
                Pawn.needs.food.CurLevel = Mathf.Clamp(Pawn.needs.food.CurLevel - extraFall60, 0f, Pawn.needs.food.MaxLevel);
            else if (Pawn.needs?.energy != null)
            {
                float extraFallEnergy60 = lactatingComp.ExtraEnergyPerDay() * factor * interval60PerDay;
                Pawn.needs.energy.CurLevel = Mathf.Clamp(Pawn.needs.energy.CurLevel - extraFallEnergy60, 0f, Pawn.needs.energy.MaxLevel);
            }
        }
        float fullnessBefore = Fullness;
        float stretchTotal = 0f;
        for (int i = 0; i < entries.Count; i++)
            stretchTotal += entries[i].Capacity * PoolModelConstants.StretchCapFactor;
        var fullnessBeforePerKey = new Dictionary<string, float>();
        if (MilkCumSettings.lactationPoolTickLog && Pawn != null)
        {
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.Key))
                    fullnessBeforePerKey[e.Key] = breastFullness.TryGetValue(e.Key, out float v) ? v : 0f;
        }
        // 按对分组、按 PairIndex 顺序：见 记忆库 design/双池与PairIndex；进水周期 60 tick
        var pairGroups = BuildPairGroupsByPairIndex(entries);
        for (int g = 0; g < pairGroups.Count; g++)
        {
            var list = pairGroups[g];
            if (list.Count == 2)
            {
                FluidPoolEntry leftE = list[0].IsLeft ? list[0] : list[1];
                FluidPoolEntry rightE = list[0].IsLeft ? list[1] : list[0];
                if (string.IsNullOrEmpty(leftE.Key) || string.IsNullOrEmpty(rightE.Key)) continue;
                float leftCap = leftE.Capacity;
                float rightCap = rightE.Capacity;
                float stretchLeft = leftCap * PoolModelConstants.StretchCapFactor;
                float stretchRight = rightCap * PoolModelConstants.StretchCapFactor;
                float curLeft = breastFullness.TryGetValue(leftE.Key, out float vl) ? vl : 0f;
                float curRight = breastFullness.TryGetValue(rightE.Key, out float vr) ? vr : 0f;
                float pressureLeft = MilkCumSettings.enablePressureFactor
                    ? MilkCumSettings.GetPressureFactor(curLeft / Mathf.Max(0.001f, stretchLeft))
                    : (curLeft >= stretchLeft ? 0f : 1f);
                float pressureRight = MilkCumSettings.enablePressureFactor
                    ? MilkCumSettings.GetPressureFactor(curRight / Mathf.Max(0.001f, stretchRight))
                    : (curRight >= stretchRight ? 0f : 1f);
                float conditionsLeft = Pawn.GetConditionsForSide(leftE.Key);
                float conditionsRight = Pawn.GetConditionsForSide(rightE.Key);
                float flowLeft = leftE.FlowMultiplier * flowPerTickScale * conditionsLeft * pressureLeft * (MilkCumSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(leftE.Key) : 1f);
                float flowRight = rightE.FlowMultiplier * flowPerTickScale * conditionsRight * pressureRight * (MilkCumSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(rightE.Key) : 1f);
                if (IsOverflowState(leftE.Key, curLeft, leftCap)) flowLeft = 0f;
                if (IsOverflowState(rightE.Key, curRight, rightCap)) flowRight = 0f;
                var pairPool = new FluidPoolState();
                pairPool.SetFrom(curLeft, curRight, 0);
                float overflow = pairPool.TickGrowth(flowLeft, flowRight, leftCap, rightCap, stretchLeft, stretchRight);
                breastFullness[leftE.Key] = pairPool.LeftFullness;
                breastFullness[rightE.Key] = pairPool.RightFullness;
                if (overflow > 0f)
                {
                    if (pairPool.LeftFullness > leftCap) overflowTriggeredByKey[leftE.Key] = true;
                    if (pairPool.RightFullness > rightCap) overflowTriggeredByKey[rightE.Key] = true;
                }
                overflowTotal += overflow;
            }
            else
            {
                foreach (var e in list)
                {
                    float stretchCap = e.Capacity * PoolModelConstants.StretchCapFactor;
                    float current = breastFullness.TryGetValue(e.Key, out float v) ? v : 0f;
                    float pressure = MilkCumSettings.enablePressureFactor
                        ? MilkCumSettings.GetPressureFactor(current / Mathf.Max(0.001f, stretchCap))
                        : (current >= stretchCap ? 0f : 1f);
                    float conditionsE = Pawn.GetConditionsForSide(e.Key);
                    float flowPerTick = e.FlowMultiplier * flowPerTickScale * conditionsE * pressure * (MilkCumSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(e.Key) : 1f);
                    if (IsOverflowState(e.Key, current, e.Capacity)) flowPerTick = 0f;
                    var (newFullness, overflow) = FluidPoolState.SingleBreastTickGrowth(current, flowPerTick, e.Capacity, stretchCap);
                    breastFullness[e.Key] = newFullness;
                    if (overflow > 0f) overflowTriggeredByKey[e.Key] = true;
                    overflowTotal += overflow;
                }
            }
        }
        float healthPercent = 1f;
        if (Pawn?.health?.summaryHealth != null)
            healthPercent = Mathf.Clamp(Pawn.health.summaryHealth.SummaryHealthPercent, 0.2f, 1f);
        float shrinkFactor = (1f - PoolModelConstants.ShrinkPerStep) * healthPercent;
        float reabsorbedPoolThisStep = 0f;
        var reabsorbedPerKey = new Dictionary<string, float>();
        foreach (var e in entries)
        {
            if (!breastFullness.TryGetValue(e.Key, out float cur)) continue;
            if (cur <= e.Capacity)
            {
                overflowTriggeredByKey[e.Key] = false;
                continue;
            }
            if (!IsOverflowState(e.Key, cur, e.Capacity)) continue;
            float shrinkAmount = (cur - e.Capacity) * (1f - shrinkFactor);
            reabsorbedPoolThisStep += shrinkAmount;
            reabsorbedPerKey[e.Key] = shrinkAmount;
            breastFullness[e.Key] = e.Capacity + (cur - e.Capacity) * shrinkFactor;
            if (breastFullness[e.Key] <= e.Capacity) overflowTriggeredByKey[e.Key] = false;
        }
        hadShrinkLastStep = reabsorbedPoolThisStep > 0f;
        float addBack = 0f;
        if (reabsorbedPoolThisStep > 0f && MilkCumSettings.reabsorbNutritionEnabled && Pawn != null)
        {
            int basis = Mathf.Clamp(MilkCumSettings.lactationExtraNutritionBasis, 0, 300);
            float factor = basis / 150f;
            addBack = reabsorbedPoolThisStep * PoolModelConstants.NutritionPerPoolUnit
                * Mathf.Clamp01(MilkCumSettings.reabsorbNutritionEfficiency) * factor;
            if (Pawn.needs?.food != null)
                Pawn.needs.food.CurLevel = Mathf.Clamp(Pawn.needs.food.CurLevel + addBack, 0f, Pawn.needs.food.MaxLevel);
            else if (Pawn.needs?.energy != null)
                Pawn.needs.energy.CurLevel = Mathf.Clamp(Pawn.needs.energy.CurLevel + addBack / MilkCumSettings.nutritionToEnergyFactor, 0f, Pawn.needs.energy.MaxLevel);
        }
        SyncLeftRightFromBreastFullness();
        var pool = new FluidPoolState();
        pool.SetFrom(leftFullness, rightFullness, ticksFullPool);
        pool.UpdateFullPoolCounter(0.95f * maxFullness, 60);
        ticksFullPool = pool.TicksFullPool;
        SyncBaseFullness();
        HandleOverflow(overflowTotal);
        TrySendFullPoolLetter();
        if (MilkCumSettings.lactationPoolTickLog && Pawn != null)
        {
            float flowAdded = Fullness - fullnessBefore + reabsorbedPoolThisStep;
            float curLevel = Pawn.needs?.food != null ? Pawn.needs.food.CurLevel : (Pawn.needs?.energy != null ? Pawn.needs.energy.CurLevel : -1f);
            MilkCumSettings.PoolTickLog($"{Pawn.Name} 饱食={curLevel:F3} 营养-{extraFall60:F4} 乳池+{flowAdded:F4} 回缩-{reabsorbedPoolThisStep:F4} 营养+{addBack:F4} 池满={Fullness:F3}");
            float totalInflow = 0f;
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                float before = fullnessBeforePerKey.TryGetValue(e.Key, out float b) ? b : 0f;
                float after = breastFullness.TryGetValue(e.Key, out float a) ? a : 0f;
                float reabsorb = reabsorbedPerKey.TryGetValue(e.Key, out float r) ? r : 0f;
                float inflow = after - before + reabsorb;
                totalInflow += inflow;
            }
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                float before = fullnessBeforePerKey.TryGetValue(e.Key, out float b) ? b : 0f;
                float after = breastFullness.TryGetValue(e.Key, out float a) ? a : 0f;
                float reabsorb = reabsorbedPerKey.TryGetValue(e.Key, out float r) ? r : 0f;
                float inflow = after - before + reabsorb;
                float nutritionShare = totalInflow >= 1E-6f ? extraFall60 * (inflow / totalInflow) : 0f;
                MilkCumSettings.PoolTickLog($"  池 {e.Key}: 营养-{nutritionShare:F4} 进+{inflow:F4} 回缩-{reabsorb:F4} 满={after:F3}");
            }
        }
    }

    /// <summary>3.3 满池事件：满池超过约 1 天且开启设置时，每 2 天最多发一封「需要挤奶」提醒信。见 Docs/泌乳系统逻辑图</summary>
    private void TrySendFullPoolLetter()
    {
        if (!MilkCumSettings.enableFullPoolLetter || Pawn == null || !Pawn.Spawned || !Pawn.IsColonyPawn()
            || ticksFullPool < 60000) return;
        int now = Find.TickManager.TicksGame;
        if (lastFullPoolLetterTick >= 0 && now - lastFullPoolLetterTick < 120000) return;
        lastFullPoolLetterTick = now;
        string title = "EM.FullPoolLetterTitle".Translate();
        if (string.IsNullOrEmpty(title) || title == "EM.FullPoolLetterTitle") title = "EM.MilkPoolFull".Translate();
        string text = "EM.FullPoolLetterText".Translate(Pawn.LabelShort);
        if (string.IsNullOrEmpty(text)) text = Pawn.LabelShort + " " + "EM.MilkPoolFull".Translate();
        Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NegativeEvent, new TargetInfo(Pawn));
    }

    /// <summary>满池溢出：累计溢出量达到阈值时生成地面污物（不扣水位）；污物 Def 由设置指定</summary>
    private void HandleOverflow(float overflowThisTick)
    {
        if (overflowThisTick <= 0f || Pawn == null || !Pawn.Spawned || Pawn.Map == null) { return; }
        overflowAccumulator += overflowThisTick;
        var filthDef = !string.IsNullOrEmpty(MilkCumSettings.overflowFilthDefName)
            ? DefDatabase<ThingDef>.GetNamedSilentFail(MilkCumSettings.overflowFilthDefName)
            : null;
        filthDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("Filth_Vomit");
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
                && MilkCumDefOf.EM_MilkOverflow != null)
                Pawn.needs.mood.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_MilkOverflow);
            if (filthSpawned > 0 && Pawn.Spawned && Pawn.Map != null)
            {
                string moteText = "EM.MilkOverflowMote".Translate();
                if (!string.IsNullOrEmpty(moteText) && moteText != "EM.MilkOverflowMote")
                    MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, moteText, 2.5f);
            }
        }
        overflowAccumulator = Mathf.Min(overflowAccumulator, PoolModelConstants.OverflowFilthThreshold * 2f);
    }

    /// <summary>
    /// 设置总奶量（0～上限）。从各乳池按比例缩放到目标总水量。
    /// </summary>
    /// <param name="value">目标总水量</param>
    /// <param name="cap">可选；不传时用 maxFullness 为上限；传时用于 clamp（如关压力因子时用撑大总容量）</param>
    public void SetFullness(float value, float? cap = null)
    {
        float target = Mathf.Clamp(value, 0f, cap ?? maxFullness);
        float total = leftFullness + rightFullness;
        if (total <= 0f) { SyncBaseFullness(); return; }
        // target >= total 时也按比例缩放，保证 leftFullness + rightFullness == target，避免与 base.fullness 不同步
        float factor = target / total;
        if (breastFullness != null)
        {
            var keys = new List<string>(breastFullness.Keys);
            foreach (var k in keys)
                breastFullness[k] = Mathf.Max(0f, breastFullness[k] * factor);
        }
        leftFullness *= factor;
        rightFullness *= factor;
        SyncBaseFullness();
    }

    /// <summary>
    /// 吸奶/挤奶时从池中扣量：按「哪对最满」优先（总满度高的对先扣），同对内先扣较满的一侧，相同时先左（与性别无关）。见 Docs/泌乳系统逻辑图；记忆库 design/双池与PairIndex、记忆库/decisions/ADR-003-选侧先左。
    /// </summary>
    /// <param name="amount">要扣的池单位量（与 Charge/Fullness 同单位）</param>
    /// <param name="drainedKeys">若非 null，会填入本次被扣量的池侧 key（用于按侧加喷乳反射刺激</param>
    /// <returns>实际扣掉的量</returns>
    public float DrainForConsume(float amount, List<string> drainedKeys = null)
    {
        if (amount <= 0f || Pawn == null) return 0f;
        breastFullness ??= new Dictionary<string, float>();
        var entries = GetCachedEntries();
        if (entries.Count == 0)
        {
            SyncLeftRightFromBreastFullness();
            SyncBaseFullness();
            return 0f;
        }
        float remaining = amount;
        // 按「该对总满度」从高到低排序，最满的对先扣
        var pairGroups = BuildPairGroupsByFullnessDescending(entries);
        for (int g = 0; g < pairGroups.Count; g++)
        {
            var list = pairGroups[g];
            if (remaining <= 0f) break;
            if (list.Count == 2)
            {
                FluidPoolEntry leftE = list[0].IsLeft ? list[0] : list[1];
                FluidPoolEntry rightE = list[0].IsLeft ? list[1] : list[0];
                if (string.IsNullOrEmpty(leftE.Key) || string.IsNullOrEmpty(rightE.Key)) continue;
                float leftF = GetFullnessForKey(leftE.Key);
                float rightF = GetFullnessForKey(rightE.Key);
                // 同对内只按满度；左右相等时先左（与性别/变性/无性别种族无关）
                bool preferLeft = true;
                bool drainLeftFirst = leftF > rightF || (Mathf.Approximately(leftF, rightF) && preferLeft);
                string firstKey = drainLeftFirst ? leftE.Key : rightE.Key;
                string secondKey = drainLeftFirst ? rightE.Key : leftE.Key;
                float firstF = drainLeftFirst ? leftF : rightF;
                float secondF = drainLeftFirst ? rightF : leftF;
                float take1 = Mathf.Min(remaining, firstF);
                if (take1 > 0f)
                {
                    breastFullness[firstKey] = Mathf.Max(0f, firstF - take1);
                    drainedKeys?.Add(firstKey);
                    remaining -= take1;
                }
                if (remaining > 0f && secondF > 0f)
                {
                    float take2 = Mathf.Min(remaining, secondF);
                    breastFullness[secondKey] = Mathf.Max(0f, secondF - take2);
                    drainedKeys?.Add(secondKey);
                    remaining -= take2;
                }
            }
            else
            {
                for (int j = 0; j < list.Count; j++)
                {
                    var e = list[j];
                    if (remaining <= 0f || string.IsNullOrEmpty(e.Key)) continue;
                    float f = GetFullnessForKey(e.Key);
                    float take = Mathf.Min(remaining, f);
                    if (take > 0f)
                    {
                        breastFullness[e.Key] = Mathf.Max(0f, f - take);
                        drainedKeys?.Add(e.Key);
                        remaining -= take;
                    }
                }
            }
        }
        // 浮点余量吸收：remaining 仅为浮点误差（< 0.001）时从池中再扣掉，使返回值与请求整数量一致，避免 1.9999999 导致少发一瓶
        const float floatDustEpsilon = 0.001f;
        if (remaining > 1E-6f && remaining < floatDustEpsilon)
        {
            foreach (var e in entries)
            {
                if (remaining <= 1E-6f || string.IsNullOrEmpty(e.Key)) break;
                float f = GetFullnessForKey(e.Key);
                if (f <= 0f) continue;
                float take = Mathf.Min(remaining, f);
                breastFullness[e.Key] = Mathf.Max(0f, f - take);
                if (drainedKeys != null && !drainedKeys.Contains(e.Key)) drainedKeys.Add(e.Key);
                remaining -= take;
            }
        }
        SyncLeftRightFromBreastFullness();
        SyncBaseFullness();
        return amount - remaining;
    }

    /// <summary>
    /// 吸奶专用：只从「当前最满的一侧」扣量（一口只能吸一侧）。选侧与 GetFirstDrainSideIndex 一致。
    /// </summary>
    public float DrainForConsumeSingleSide(float amount, List<string> drainedKeys = null)
    {
        if (amount <= 0f || Pawn == null) return 0f;
        breastFullness ??= new Dictionary<string, float>();
        var entries = GetCachedEntries();
        if (entries.Count == 0)
        {
            SyncLeftRightFromBreastFullness();
            SyncBaseFullness();
            return 0f;
        }
        int idx = GetFirstDrainSideIndex();
        if (idx < 0 || idx >= entries.Count) { SyncBaseFullness(); return 0f; }
        string singleKey = entries[idx].Key;
        float singleFullness = GetFullnessForKey(singleKey);
        if (string.IsNullOrEmpty(singleKey) || singleFullness <= 0f) { SyncBaseFullness(); return 0f; }
        float take = Mathf.Min(amount, singleFullness);
        breastFullness[singleKey] = Mathf.Max(0f, singleFullness - take);
        drainedKeys?.Add(singleKey);
        SyncLeftRightFromBreastFullness();
        SyncBaseFullness();
        return take;
    }

    /// <summary>
    /// 机器挤奶专用：每侧按「该侧流速」独立并行扣量；总扣量不超过 remainingCap。每侧本 tick 最多扣 ratePerSidePerTick[i]（与 GetCachedEntries 顺序一致），若各侧拟扣量之和超过 remainingCap 则按比例缩减。用于左 2.5 秒、右 1 秒同步进行，总耗时由最慢的一侧决定。
    /// </summary>
    /// <param name="ratePerSidePerTick">每侧本 tick 最多扣的池单位量，顺序同 GetCachedEntries()</param>
    /// <param name="remainingCap">本 tick 总扣量上限（通常 = amountToTake - totalDrained）</param>
    /// <param name="drainedKeys">若非 null，会填入本次被扣量的池侧 key</param>
    /// <returns>实际扣掉的总量</returns>
    public float DrainForConsumeParallel(IList<float> ratePerSidePerTick, float remainingCap, List<string> drainedKeys = null)
    {
        if (ratePerSidePerTick == null || ratePerSidePerTick.Count == 0 || remainingCap <= 0f || Pawn == null) return 0f;
        breastFullness ??= new Dictionary<string, float>();
        var entries = GetCachedEntries();
        if (entries.Count == 0)
        {
            SyncLeftRightFromBreastFullness();
            SyncBaseFullness();
            return 0f;
        }
        float totalWouldTake = 0f;
        var takes = new List<float>(entries.Count);
        for (int i = 0; i < entries.Count && i < ratePerSidePerTick.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) { takes.Add(0f); continue; }
            float f = GetFullnessForKey(e.Key);
            float take = Mathf.Min(ratePerSidePerTick[i], f);
            takes.Add(take);
            totalWouldTake += take;
        }
        while (takes.Count < entries.Count) takes.Add(0f);
        float factor = totalWouldTake > remainingCap && totalWouldTake > 1E-6f ? remainingCap / totalWouldTake : 1f;
        float totalDrained = 0f;
        for (int i = 0; i < entries.Count && i < takes.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            float take = takes[i] * factor;
            if (take <= 0f) continue;
            float cur = GetFullnessForKey(e.Key);
            take = Mathf.Min(take, cur);
            if (take > 0f)
            {
                breastFullness[e.Key] = Mathf.Max(0f, cur - take);
                drainedKeys?.Add(e.Key);
                totalDrained += take;
            }
        }
        SyncLeftRightFromBreastFullness();
        SyncBaseFullness();
        return totalDrained;
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
        // JobDriver 传累加小数（2.3、0.98 等），向下取整得瓶数；0.999f 容错避免 1.999… 少发一瓶
        int num = Mathf.FloorToInt(totalDrained);
        if (totalDrained - num >= 0.999f)
            num++;
        if (num > 0)
        {
            pawn.LactatingHediffWithComps()?.OnGathered();
            while (num > 0)
            {
                int stack = Mathf.Clamp(num, 1, ResourceDef.stackLimit);
                num -= stack;
                Thing thing = ThingMaker.MakeThing(ResourceDef);
                if (thing.TryGetComp<CompShowProducer>() is CompShowProducer compShowProducer && pawn.RaceProps.Humanlike)
                {
                    if (MilkCumSettings.HasRaceTag(thing))
                        compShowProducer.producerKind = pawn.kindDef;
                    if (MilkCumSettings.HasPawnTag(thing))
                        compShowProducer.producer = pawn;
                }
                thing.stackCount = stack;
                if (milkingSpot != null)
                    milkingSpot.PlaceMilkThing(thing);
                else
                    GenPlace.TryPlaceThing(thing, doer.Position, doer.Map, ThingPlaceMode.Near);
            }
        }
        else if (totalDrained >= 0.001f && totalDrained < 1f && MilkCumDefOf.EM_HumanMilkPartial != null && ResourceDef == MilkCumDefOf.EM_HumanMilk)
        {
            // 未满 1 瓶：生成「人奶(未满)」，营养 = totalDrained，可继续存放/食用
            pawn.LactatingHediffWithComps()?.OnGathered();
            Thing thing = ThingMaker.MakeThing(MilkCumDefOf.EM_HumanMilkPartial);
            if (thing.TryGetComp<CompPartialMilk>() is CompPartialMilk compPartial)
                compPartial.fillAmount = totalDrained;
            if (thing.TryGetComp<CompShowProducer>() is CompShowProducer compShowProducer && pawn.RaceProps.Humanlike)
            {
                if (MilkCumSettings.HasRaceTag(thing))
                    compShowProducer.producerKind = pawn.kindDef;
                if (MilkCumSettings.HasPawnTag(thing))
                    compShowProducer.producer = pawn;
            }
            if (milkingSpot != null)
                milkingSpot.PlaceMilkThing(thing);
            else
                GenPlace.TryPlaceThing(thing, doer.Position, doer.Map, ThingPlaceMode.Near);
        }
        SyncBaseFullness();
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

    /// <summary>是否有躯干/乳房损伤；委托给 MilkRelatedHealthHelper，供炎症模型等使用</summary>
    public static bool HasTorsoOrBreastInjury(Pawn pawn) => MilkRelatedHealthHelper.HasTorsoOrBreastInjury(pawn);
    /// <summary>是否允许被该泌乳者自动喂食。仅看 canBeFed 开关；不再使用「谁可以喂我」列表</summary>
    public bool AllowedToBeAutoFedBy(Pawn pawn)
    {
        return this.MilkSettings?.canBeFed == true;
    }
}
