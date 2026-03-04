using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Milk.Data;
using MilkCum.Milk.Helpers;
using MilkCum.Milk.Jobs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Milk.Comps;
public class CompEquallyMilkable : CompMilkable
{
    protected Pawn Pawn => parent as Pawn;
    /// <summary>产出周期已移除，挤奶间隔固定为 1 天（水池模型不依赖此值）。</summary>
    protected override int GatherResourcesIntervalDays => 1;
    protected override int ResourceAmount => (int)Pawn.MilkAmount();
    protected override ThingDef ResourceDef => Pawn.MilkDef();
    protected virtual float fResourceAmount => Pawn.MilkAmount();
    public float breastfedAmount = 0f;
    public float maxFullness = 1f;

    /// <summary>水池模型：左乳水位（0～左乳容量），与右乳合计 0～maxFullness；总容量 = 左+右（正常人 1+1=2）。由 SyncLeftRightFromBreastFullness 从 breastFullness 汇总。</summary>
    private float leftFullness;
    /// <summary>水池模型：右乳水位（0～右乳基础容量），与左乳合计 0～maxFullness。</summary>
    private float rightFullness;
    /// <summary>按单乳 key（Part.def.defName 或 defName_L/R）的水位，用于左1/右1/左2/右2 独立进水与展示。有则优先；无则从 left/right 反推。</summary>
    private Dictionary<string, float> breastFullness = new Dictionary<string, float>();
    /// <summary>未启用 RJW 乳房尺寸时单侧固定容量 0.5；总容量 = 左 + 右。</summary>
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
    /// <summary>规格：连续满池（Fullness≥95% maxFullness）的 tick 数，用于乳腺炎/堵塞触发。</summary>
    private int ticksFullPool;
    /// <summary>3.3 满池事件：上次发送「需要挤奶」信件的 tick，避免刷屏。</summary>
    private int lastFullPoolLetterTick = -1;
    /// <summary>四层模型（阶段3.2）：组织适应导致的容量增量 ≥0，maxFullness = 基础容量 + 本值。</summary>
    private float capacityAdaptation;
    /// <summary>2.3：药物泌乳状态缓存，每 30 tick 仅状态变化时增删 EM_DrugLactationBurden/EM_LactatingGain，减少重复查找。</summary>
    private bool cachedWasLactatingWithDrugInduced = false;
    private bool cachedGainConditionsMet = false;
    /// <summary>满池溢出累计量（仅 Debug 显示用）。</summary>
    internal float OverflowAccumulator => overflowAccumulator;

    /// <summary>按 key 取该乳当前水位，用于健康页悬停等；无该 key 时返回 0。</summary>
    public float GetFullnessForKey(string key)
    {
        if (string.IsNullOrEmpty(key) || breastFullness == null) return 0f;
        return breastFullness.TryGetValue(key, out float v) ? v : 0f;
    }

    /// <summary>回缩吸收：当前若有任一侧水位超过基础容量，每 30 tick 会回缩一部分（不溢出）；本方法将该回缩量折算为「每日补充营养」，供 Need_Food 补丁增加饱食度。仅当设置开启且存在回缩时返回 &gt; 0。</summary>
    public float GetReabsorbedNutritionPerDay()
    {
        if (!EqualMilkingSettings.reabsorbNutritionEnabled || Pawn == null || breastFullness == null) return 0f;
        var entries = Pawn.GetBreastPoolEntries();
        if (entries == null || entries.Count == 0) return 0f;
        float healthPercent = 1f;
        if (Pawn.health?.summaryHealth != null)
            healthPercent = Mathf.Clamp(Pawn.health.summaryHealth.SummaryHealthPercent, 0.2f, 1f);
        float shrinkFactor = (1f - PoolModelConstants.ShrinkPerStep) * healthPercent;
        float reabsorbedPoolPer30Tick = 0f;
        foreach (var e in entries)
        {
            if (!breastFullness.TryGetValue(e.Key, out float cur)) continue;
            if (cur > e.Capacity)
                reabsorbedPoolPer30Tick += (cur - e.Capacity) * (1f - shrinkFactor);
        }
        if (reabsorbedPoolPer30Tick <= 0f) return 0f;
        float reabsorbedPoolPerDay = reabsorbedPoolPer30Tick * (60000f / 30f);
        return reabsorbedPoolPerDay * PoolModelConstants.NutritionPerPoolUnit
            * Mathf.Clamp01(EqualMilkingSettings.reabsorbNutritionEfficiency);
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
            ? breastFullness.Keys.ToList()
            : new List<string>();
        List<float> breastVals = (Scribe.mode == LoadSaveMode.Saving && breastFullness != null)
            ? breastFullness.Values.ToList()
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
        Scribe_Collections.Look(ref assignedFeeders, "CanBeFedBy", LookMode.Reference);
        Scribe_Collections.Look(ref allowedSucklers, "AllowedSucklers", LookMode.Reference);
        Scribe_Collections.Look(ref allowedConsumers, "AllowedConsumers", LookMode.Reference);
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

    /// <summary>从 per-breast 字典汇总到 leftFullness / rightFullness（按 GetBreastPoolEntries 的 IsLeft）。</summary>
    private void SyncLeftRightFromBreastFullness()
    {
        if (Pawn == null || breastFullness == null) return;
        var entries = Pawn.GetBreastPoolEntries();
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

    /// <summary>泌乳结束时清空双池（由 HediffComp_EqualMilkingLactating 调用）。</summary>
    public void ClearPools()
    {
        leftFullness = 0f;
        rightFullness = 0f;
        breastFullness?.Clear();
        SyncBaseFullness();
    }
    /// <summary>确保列表非 null、移除无效引用；名单为空时预填子女+伴侣（默认勾选）。</summary>
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
    /// <summary>仅做纯判断，不修改健康系统。Hediff 的增删在 CompTick 的 EnsureLactatingHediffFromConditions 中执行。</summary>
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
            cachedActive = pawn.IsLactating() && pawn.IsMilkable();
            updateTick = Find.TickManager.TicksGame + 500;
            return cachedActive;
        }
    }

    public override void CompTick()
    {
        if (!parent.IsHashIntervalTick(30)) { return; }
        // 每 tick 同步总容量（单乳 0.5 / 双乳 (左+右Severity)/2），供满池判定与显示
        if (parent is Pawn p)
        {
            float baseMax = Mathf.Max(0.01f, p.GetLeftBreastCapacityFactor() + p.GetRightBreastCapacityFactor());
            if (EqualMilkingSettings.enableTissueAdaptation)
            {
                float effectiveMax = baseMax + capacityAdaptation;
                float P = effectiveMax > 0f ? Mathf.Clamp01(Fullness / effectiveMax) : 0f;
                float step = 30f / 60000f; // 每 30 tick = 0.0005 游戏日
                float theta = Mathf.Max(0f, EqualMilkingSettings.adaptationTheta);
                float omega = Mathf.Max(0f, EqualMilkingSettings.adaptationOmega);
                capacityAdaptation += step * (theta * Mathf.Max(P - 0.85f, 0f) - omega * (1f - P));
                float capMax = baseMax * Mathf.Clamp(EqualMilkingSettings.adaptationCapMaxRatio, 0f, 1f);
                capacityAdaptation = Mathf.Clamp(capacityAdaptation, 0f, capMax);
            }
            maxFullness = baseMax + capacityAdaptation;
            if (leftFullness + rightFullness > maxFullness)
                SetFullness(maxFullness);
        }
        EnsureLactatingHediffFromConditions();
        ApplyDrugInducedLactationEffects();
        if (!Active) { return; }
        // LOD：非当前地图的 pawn 每 300 tick 更新一次池，降低规模大时的负担；需结合 Profiler 验证。
        bool onCurrentMap = Pawn?.MapHeld == null || Pawn.MapHeld == Find.CurrentMap;
        if (onCurrentMap || parent.IsHashIntervalTick(300))
            UpdateMilkPools();
        if (parent.IsHashIntervalTick(2000)) TryTriggerMastitis();
        UpdateHealthHediffs();
    }

    /// <summary>根据基因/物种/设置维护 Lactating Hediff（增删与 Severity），仅在 CompTick 调用，避免 Active getter 产生副作用。</summary>
    private void EnsureLactatingHediffFromConditions()
    {
        if (parent is not Pawn pawn || !pawn.SpawnedOrAnyParentSpawned || !pawn.IsColonyPawn() || pawn.Faction == null)
            return;
        // 永久泌乳基因：确保有 Lactating 并维持高 severity
        if (pawn.genes?.HasActiveGene(EMDefOf.EM_Permanent_Lactation) == true)
        {
            Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating, pawn.GetBreastOrChestPart());
            lactating.Severity = Mathf.Max(lactating.Severity, 0.9999f);
            return;
        }
        // 非人形且不可挤奶：移除 Lactating
        if (!pawn.RaceProps.Humanlike && !pawn.IsMilkable())
        {
            if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating) is Hediff lactating)
                pawn.health.RemoveHediff(lactating);
            return;
        }
        // 设置：殖民地成年雌性动物始终泌乳
        if (EqualMilkingSettings.femaleAnimalAdultAlwaysLactating && pawn.IsAdultFemaleAnimalOfColony())
        {
            Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating, pawn.GetBreastOrChestPart());
            lactating.Severity = Mathf.Max(lactating.Severity, 1f);
        }
    }

    /// <summary>药物诱发泌乳负担与增益 Hediff；不依赖 Active，以便停止泌乳或失去耐受/成瘾时能移除。状态变化时才增删 Hediff，减少每 30 tick 的重复查找。见 Docs/泌乳系统逻辑图。</summary>
    private void ApplyDrugInducedLactationEffects()
    {
        if (Pawn == null || !Pawn.RaceProps.Humanlike) return;
        bool nowLactatingWithDrug = Pawn.IsLactating() && Pawn.HasDrugInducedLactation();
        bool nowGainConditions = nowLactatingWithDrug && EqualMilkingSettings.lactatingGainEnabled && EqualMilkingSettings.lactatingGainCapModPercent > 0f;
        if (cachedWasLactatingWithDrugInduced == nowLactatingWithDrug && cachedGainConditionsMet == nowGainConditions)
            return;
        cachedWasLactatingWithDrugInduced = nowLactatingWithDrug;
        cachedGainConditionsMet = nowGainConditions;

        if (EMDefOf.EM_DrugLactationBurden != null)
        {
            if (nowLactatingWithDrug)
            {
                if (Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_DrugLactationBurden) == null)
                    Pawn.health.AddHediff(EMDefOf.EM_DrugLactationBurden, Pawn.GetBreastOrChestPart());
            }
            else if (Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_DrugLactationBurden) is Hediff burden)
                Pawn.health.RemoveHediff(burden);
        }
        if (EMDefOf.EM_LactatingGain != null)
        {
            if (nowGainConditions)
            {
                if (Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_LactatingGain) == null)
                    Pawn.health.AddHediff(EMDefOf.EM_LactatingGain, Pawn.GetBreastOrChestPart());
            }
            else if (Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_LactatingGain) is Hediff gain)
                Pawn.health.RemoveHediff(gain);
        }
    }

    /// <summary>水池模型：按对进水；每对仅当两侧都达基础容量后才允许撑大（见 Docs/泌乳系统逻辑图）。满池后不再进水（flow=0），与 GetFlowPerDay 返回 0 一致，能量有据；回缩仍执行，回缩吸收由 GetReabsorbedNutritionPerDay 折算饱食度。</summary>
    private void UpdateMilkPools()
    {
        var lactatingComp = Pawn?.LactatingHediffWithComps()?.comps?.OfType<HediffComp_EqualMilkingLactating>().FirstOrDefault();
        if (lactatingComp == null || lactatingComp.RemainingDays <= 0f) { return; }
        float currentLactation = lactatingComp.CurrentLactationAmount;
        float hungerFactor = PawnUtility.BodyResourceGrowthSpeed(Pawn);
        if (currentLactation <= 0f || hungerFactor <= 0f) { return; }
        float drive = EqualMilkingSettings.GetEffectiveDrive(currentLactation);
        float basePerDay = drive * hungerFactor
            * Pawn.GetMilkFlowMultiplierFromConditions()
            * Pawn.GetMilkFlowMultiplierFromGenes()
            * EqualMilkingSettings.defaultFlowMultiplierForHumanlike;
        var entries = Pawn.GetBreastPoolEntries();
        breastFullness ??= new Dictionary<string, float>();
        float overflowTotal = 0f;
        float flowPerTickScale = basePerDay / 60000f * 30f;
        SyncLeftRightFromBreastFullness();
        // 四层模型：压力按「哪对乳房的哪一侧」分别计算（该侧满度/该侧撑大容量），满则压低该侧进水流速；未启用时总满则硬停
        if (!EqualMilkingSettings.enablePressureFactor && Fullness >= maxFullness)
            flowPerTickScale = 0f;
        // 喷乳反射 R：按侧生效，每侧进水量 × GetLetdownReflexFlowMultiplier(sideKey)；每 tick 统一衰减各侧 R
        if (EqualMilkingSettings.enableLetdownReflex)
            lactatingComp.DecayLetdownReflex(30f / 60f); // Δt = 30 tick = 0.5 分钟
        // 四层模型：炎症 I 离散更新（每 30 tick，Δt = 30/3600 小时）
        if (EqualMilkingSettings.enableInflammationModel)
        {
            float maxF = Mathf.Max(0.001f, maxFullness);
            lactatingComp.UpdateInflammation(Fullness / maxF, 30f / 3600f);
            lactatingComp.TryTriggerMastitisFromInflammation();
        }
        if (EqualMilkingSettings.enableToleranceDynamic)
            lactatingComp.UpdateToleranceDynamic(currentLactation, 30f / 60000f);
        // 按对分组、每对 TickGrowth：见 记忆库/design/双池与PairIndex；进水周期 30 tick：见 记忆库/decisions/ADR-001-进水与衰减周期
        var byPair = entries.GroupBy(e => e.PairIndex).OrderBy(g => g.Key).ToList();
        foreach (var group in byPair)
        {
            var list = group.ToList();
            if (list.Count == 2)
            {
                var leftE = list.FirstOrDefault(e => e.IsLeft);
                var rightE = list.FirstOrDefault(e => !e.IsLeft);
                if (string.IsNullOrEmpty(leftE.Key) || string.IsNullOrEmpty(rightE.Key)) continue;
                float leftCap = leftE.Capacity;
                float rightCap = rightE.Capacity;
                float stretchLeft = leftCap * PoolModelConstants.StretchCapFactor;
                float stretchRight = rightCap * PoolModelConstants.StretchCapFactor;
                float curLeft = breastFullness.TryGetValue(leftE.Key, out float vl) ? vl : 0f;
                float curRight = breastFullness.TryGetValue(rightE.Key, out float vr) ? vr : 0f;
                float pressureLeft = EqualMilkingSettings.enablePressureFactor
                    ? EqualMilkingSettings.GetPressureFactor(curLeft / Mathf.Max(0.001f, stretchLeft))
                    : (curLeft >= stretchLeft ? 0f : 1f);
                float pressureRight = EqualMilkingSettings.enablePressureFactor
                    ? EqualMilkingSettings.GetPressureFactor(curRight / Mathf.Max(0.001f, stretchRight))
                    : (curRight >= stretchRight ? 0f : 1f);
                float conditionsLeft = Pawn.GetConditionsForSide(leftE.Key);
                float conditionsRight = Pawn.GetConditionsForSide(rightE.Key);
                float flowLeft = leftE.FlowMultiplier * flowPerTickScale * conditionsLeft * pressureLeft * (EqualMilkingSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(leftE.Key) : 1f);
                float flowRight = rightE.FlowMultiplier * flowPerTickScale * conditionsRight * pressureRight * (EqualMilkingSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(rightE.Key) : 1f);
                var pairPool = new LactationPoolState();
                pairPool.SetFrom(curLeft, curRight, 0);
                float overflow = pairPool.TickGrowth(flowLeft, flowRight, leftCap, rightCap, stretchLeft, stretchRight);
                breastFullness[leftE.Key] = pairPool.LeftFullness;
                breastFullness[rightE.Key] = pairPool.RightFullness;
                overflowTotal += overflow;
            }
            else
            {
                foreach (var e in list)
                {
                    float stretchCap = e.Capacity * PoolModelConstants.StretchCapFactor;
                    float current = breastFullness.TryGetValue(e.Key, out float v) ? v : 0f;
                    float pressure = EqualMilkingSettings.enablePressureFactor
                        ? EqualMilkingSettings.GetPressureFactor(current / Mathf.Max(0.001f, stretchCap))
                        : (current >= stretchCap ? 0f : 1f);
                    float conditionsE = Pawn.GetConditionsForSide(e.Key);
                    float flowPerTick = e.FlowMultiplier * flowPerTickScale * conditionsE * pressure * (EqualMilkingSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(e.Key) : 1f);
                    var (newFullness, overflow) = LactationPoolState.SingleBreastTickGrowth(current, flowPerTick, e.Capacity, stretchCap);
                    breastFullness[e.Key] = newFullness;
                    overflowTotal += overflow;
                }
            }
        }
        float healthPercent = 1f;
        if (Pawn?.health?.summaryHealth != null)
            healthPercent = Mathf.Clamp(Pawn.health.summaryHealth.SummaryHealthPercent, 0.2f, 1f);
        float shrinkFactor = (1f - PoolModelConstants.ShrinkPerStep) * healthPercent;
        foreach (var e in entries)
        {
            if (!breastFullness.TryGetValue(e.Key, out float cur)) continue;
            if (cur > e.Capacity)
                breastFullness[e.Key] = e.Capacity + (cur - e.Capacity) * shrinkFactor;
        }
        SyncLeftRightFromBreastFullness();
        var pool = new LactationPoolState();
        pool.SetFrom(leftFullness, rightFullness, ticksFullPool);
        pool.UpdateFullPoolCounter(0.95f * maxFullness, 30);
        ticksFullPool = pool.TicksFullPool;
        SyncBaseFullness();
        HandleOverflow(overflowTotal);
        TrySendFullPoolLetter();
    }

    /// <summary>3.3 满池事件：满池超过约 1 天且开启设置时，每 2 天最多发一封「需要挤奶」提醒信。见 Docs/泌乳系统逻辑图。</summary>
    private void TrySendFullPoolLetter()
    {
        if (!EqualMilkingSettings.enableFullPoolLetter || Pawn == null || !Pawn.Spawned || !Pawn.IsColonyPawn()
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

    /// <summary>满池时添加「乳房胀满」hediff；低于 90% maxFullness 时移除（滞后避免抖动）。</summary>
    private void UpdateHealthHediffs()
    {
        if (EMDefOf.EM_BreastsEngorged == null || Pawn == null || !Pawn.RaceProps.Humanlike || !Pawn.IsLactating()) return;
        var engorged = Pawn.health.hediffSet.GetFirstHediffOfDef(EMDefOf.EM_BreastsEngorged);
        float fullThreshold = 0.95f * maxFullness;
        float lowThreshold = 0.9f * maxFullness;
        if (Fullness >= fullThreshold && engorged == null)
            Pawn.health.AddHediff(EMDefOf.EM_BreastsEngorged, Pawn.GetBreastOrChestPart());
        else if (Fullness < lowThreshold && engorged != null)
            Pawn.health.RemoveHediff(engorged);
    }
    /// <summary>
    /// 设置总奶量（0～1）。从各乳池按比例缩放到目标总水量。
    /// </summary>
    public void SetFullness(float value)
    {
        float target = Mathf.Clamp(value, 0f, maxFullness);
        float total = leftFullness + rightFullness;
        if (total <= 0f) { SyncBaseFullness(); return; }
        // target >= total 时也按比例缩放，保证 leftFullness + rightFullness == target，避免与 base.fullness 不同步
        float factor = target / total;
        if (breastFullness != null)
        {
            var keys = breastFullness.Keys.ToList();
            foreach (var k in keys)
                breastFullness[k] = Mathf.Max(0f, breastFullness[k] * factor);
        }
        leftFullness *= factor;
        rightFullness *= factor;
        SyncBaseFullness();
    }

    /// <summary>
    /// 吸奶/挤奶时从池中扣量：按「哪对最满」优先（总满度高的对先扣），同对内先扣较满的一侧，相同时先左（与性别无关）。见 Docs/泌乳系统逻辑图；记忆库/design/双池与PairIndex、记忆库/decisions/ADR-003-选侧先左。
    /// </summary>
    /// <param name="amount">要扣的池单位量（与 Charge/Fullness 同单位）</param>
    /// <param name="drainedKeys">若非 null，会填入本次被扣量的池侧 key（用于按侧加喷乳反射刺激）</param>
    /// <returns>实际扣掉的量</returns>
    public float DrainForConsume(float amount, List<string> drainedKeys = null)
    {
        if (amount <= 0f || Pawn == null) return 0f;
        breastFullness ??= new Dictionary<string, float>();
        var entries = Pawn.GetBreastPoolEntries();
        if (entries.Count == 0)
        {
            SyncLeftRightFromBreastFullness();
            SyncBaseFullness();
            return 0f;
        }
        float remaining = amount;
        // 按「该对总满度」从高到低排序，最满的对先扣，多对时轮转自然、后面的对也能被吸到
        var byPair = entries.GroupBy(e => e.PairIndex)
            .OrderByDescending(g => g.Sum(e => GetFullnessForKey(e.Key)))
            .ToList();
        foreach (var group in byPair)
        {
            if (remaining <= 0f) break;
            var list = group.ToList();
            if (list.Count == 2)
            {
                var leftE = list.FirstOrDefault(e => e.IsLeft);
                var rightE = list.FirstOrDefault(e => !e.IsLeft);
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
                foreach (var e in list)
                {
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
        SyncLeftRightFromBreastFullness();
        SyncBaseFullness();
        return amount - remaining;
    }
    /// <summary>
    /// 挤奶：从所有乳房对按顺序扣量（与 DrainForConsume 一致），再按实际扣量产出奶。
    /// </summary>
    new public void Gathered(Pawn doer)
    {
        if (!Active)
        {
            Log.Error(string.Concat(doer, " gathered body resources while not Active: ", parent));
        }
        Pawn pawn = parent as Pawn;
        float amountToTake = Mathf.Min(fResourceAmount, Fullness);
        if (amountToTake <= 0f)
        {
            SyncBaseFullness();
            return;
        }
        var drainedKeys = new List<string>();
        float drained = DrainForConsume(amountToTake, drainedKeys);
        pawn.LactatingHediffWithComps()?.OnGatheredLetdownByKeys(drainedKeys);
        float yieldFactor = doer.GetStatValue(StatDefOf.AnimalGatherYield);
        Building_Milking milkingSpot = (doer.jobs?.curDriver as JobDriver_EquallyMilk)?.MilkBuilding;
        if (milkingSpot != null)
            yieldFactor += milkingSpot.YieldOffset();
        // AnimalGatherYield 为数量倍率（原版 1.5+），非成功概率；effectiveYield = drained × yieldFactor，上限 drained。
        float effectiveYield = Mathf.Min(drained, drained * Mathf.Max(0f, yieldFactor));
        int num = GenMath.RoundRandom(effectiveYield);
        if (num <= 0 && parent.Map != null && effectiveYield < drained)
        {
            MoteMaker.ThrowText((doer.DrawPos + parent.DrawPos) / 2f, parent.Map, Lang.ProductWasted, 3.65f);
        }
        else if (num > 0)
        {
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
        SyncBaseFullness();
        // 10.8-5：挤奶心情细分 — 被允许的人挤奶给正面记忆，否则给强制挤奶负面记忆
        if (parent is Pawn milkedPawn && milkedPawn.RaceProps.Humanlike && milkedPawn.needs?.mood?.thoughts?.memories != null)
        {
            if (ExtensionHelper.IsAllowedSuckler(milkedPawn, doer))
            {
                if (EMDefOf.EM_AllowedMilking != null)
                    milkedPawn.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_AllowedMilking);
                if (milkedPawn.HasDrugInducedLactation() && EMDefOf.EM_Prolactin_Joy != null)
                    milkedPawn.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_Prolactin_Joy);
            }
            else if (EMDefOf.EM_ForcedMilking != null)
                milkedPawn.needs.mood.thoughts.memories.TryGainMemory(EMDefOf.EM_ForcedMilking);
        }
        // 10.8-6：记录最近一次挤奶时刻，供「长时间未挤奶」心情判定
        lastGatheredTick = Find.TickManager.TicksGame;
        // 规格：挤奶时略微缓解乳房不适/乳腺炎
        if (parent is Pawn pawnForMastitis && EMDefOf.EM_Mastitis != null)
        {
            var mastitis = pawnForMastitis.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_Mastitis);
            if (mastitis != null && mastitis.Severity > 0.01f)
            {
                mastitis.Severity = Mathf.Max(0f, mastitis.Severity - 0.05f);
                if (mastitis.Severity <= 0f)
                    pawnForMastitis.health.RemoveHediff(mastitis);
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

    public static bool HasTorsoOrBreastInjury(Pawn pawn)
    {
        if (pawn?.health?.hediffSet?.hediffs == null) return false;
        foreach (var h in pawn.health.hediffSet.hediffs)
        {
            if (h.Part?.def?.defName == null) continue;
            string dn = h.Part.def.defName;
            if (dn.StartsWith("Torso") || dn.StartsWith("Breast") || dn.StartsWith("Chest")) return true;
        }
        return false;
    }
    /// <summary>是否允许被该泌乳者自动喂食。仅看 canBeFed 开关；不再使用「谁可以喂我」列表。</summary>
    public bool AllowedToBeAutoFedBy(Pawn pawn)
    {
        return this.MilkSettings?.canBeFed == true;
    }
}
