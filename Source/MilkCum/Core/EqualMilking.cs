using Verse;
using RimWorld;
using MilkCum.Milk.Helpers;
using MilkCum.Milk.HarmonyPatches;
using MilkCum.Milk.Jobs;
using MilkCum.Milk.Givers;
using MilkCum.Milk.Comps;
using System;

namespace MilkCum.Core;
/// <summary>
/// 药物效果系统修复类
/// 实现用户提出的递减累加机制
/// </summary>
public static class DrugEffectSystemFix
{
    // 核心参数配置
    public const float BASE_PRODUCTION_MULTIPLIER = 1.0f;
    public const float INITIAL_BONUS = 0.3f;        // 初始加成0.3
    public const float DECAY_FACTOR = 0.9f;         // 每次递减10%
    public const float DAILY_DECAY = 0.1f;          // 每日自然衰退0.1
    public const float TOLERANCE_DECAY = 0.9f;      // 耐受性效果递减
    public const float NATURAL_DECAY_RATE = 0.067f;  // 自然衰减率(约15天完全消退)
    public const float TOLERANCE_DECAY_BOOST = 0.033f; // 耐受性额外衰减加成
    
    /// <summary>
    /// 计算累积药物效果
    /// </summary>
    public static float CalculateCumulativeProduction(int usageCount)
    {
        // 累积加成计算：1 + 0.3 + 0.27 + 0.243 + ...
        float cumulativeBonus = 0f;
        for (int i = 1; i <= usageCount; i++)
        {
            float currentBonus = INITIAL_BONUS * (float)Math.Pow(DECAY_FACTOR, i - 1);
            cumulativeBonus += currentBonus;
        }
        
        // 耐受性修正
        float toleranceEffect = usageCount > 0 ? 
            (float)Math.Pow(TOLERANCE_DECAY, usageCount - 1) : 1.0f;
        
        // 最终效果 = (1 + 累积加成) × 耐受性修正
        return (BASE_PRODUCTION_MULTIPLIER + cumulativeBonus) * toleranceEffect;
    }
    
    /// <summary>
    /// 应用指数衰减（自然衰减 + 耐受衰减）
    /// </summary>
    public static float ApplyExponentialDecay(float currentValue, int daysSinceLastUse, float toleranceSeverity)
    {
        // 计算综合衰减率
        float totalDecayRate = NATURAL_DECAY_RATE + (toleranceSeverity * TOLERANCE_DECAY_BOOST);
        
        // 指数衰减公式：newValue = oldValue × (1 - decayRate)^days
        float decayFactor = (float)Math.Pow(1 - totalDecayRate, daysSinceLastUse);
        float decayedValue = currentValue * decayFactor;
        
        // 确保不低于基础值
        return Math.Max(BASE_PRODUCTION_MULTIPLIER, decayedValue);
    }
    
    /// <summary>
    /// 计算衰减后的效果值
    /// </summary>
    public static float CalculateDecayedEffect(float baseEffect, int daysPassed, float toleranceLevel)
    {
        // 基础效果 × 衰减因子
        float naturalDecay = (float)Math.Pow(1 - NATURAL_DECAY_RATE, daysPassed);
        float toleranceDecay = (float)Math.Pow(1 - (toleranceLevel * TOLERANCE_DECAY_BOOST), daysPassed);
        
        return baseEffect * naturalDecay * toleranceDecay;
    }
}

[StaticConstructorOnStartup]
public static class EqualMilking
{
    static EqualMilking()
    {
        LongEventHandler.QueueLongEvent(() => { EventHelper.TriggerPostLoadLong(); }, "EqualMilking_LongEvent", false, null);

        EventHelper.OnPostLoadLong += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnPostNewGame += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnPostLoadGame += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnSettingsChanged += GeneHelper.ReloadImpliedGenes;
        EventHelper.OnPostLoadLong += Init;
        // Patch vanilla
        EqualMilkingMod.Harmony.PatchAll();
        ProlactinAddictionPatch.ApplyIfPossible(EqualMilkingMod.Harmony);
        CumpilationIntegration.ApplyPatches(EqualMilkingMod.Harmony);
    }
    public static void Init()
    {
        JobDefOf.Milk.driverClass = typeof(JobDriver_EquallyMilk);
        DefDatabase<WorkGiverDef>.GetNamed("Milk").giverClass = typeof(WorkGiver_EquallyMilk);
        HediffDefOf.Lactating.hediffClass = typeof(HediffWithComps_EqualMilkingLactating);
        // 确保耐受 Def 的 hediffClass 在运行时被正确设置，避免 Def 加载时类型未解析导致 MakeHediff 时 type 为 null
        if (EMDefOf.EM_Prolactin_Tolerance != null)
            EMDefOf.EM_Prolactin_Tolerance.hediffClass = typeof(Hediff_ProlactinTolerance);
        StatCategoryDefOf.AnimalProductivity.displayAllByDefault = true;

        // label auto translations
        if (DefDatabase<ThingDef>.GetNamedSilentFail("VCE_Cheese") is ThingDef cheese)
        {
            ThingDef humanMilkCheese = DefDatabase<ThingDef>.GetNamed("VCE_HumanMilkCheese");
            humanMilkCheese.label = Lang.Join(Lang.Human, Lang.Milk, cheese.label);
            humanMilkCheese.description = cheese.description;
        }
    }
}
