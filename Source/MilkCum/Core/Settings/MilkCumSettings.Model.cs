using UnityEngine;
using Verse;

namespace MilkCum.Core.Settings;

/// <summary>`MilkCumSettings` 的流体模型参数、公式方法与序列化分块。</summary>
internal partial class MilkCumSettings
{
	/// <summary>近满/顶满撑大时（≥满池阈值×撑大容量）压力系数下限抬升倍率（0~1）。</summary>
	public static float overflowResidualFlowFactor = 0.04f;
	/// <summary>残余压力是否随泌乳量 L、炎症 I 缩放；关闭时仅用 overflowResidualFlowFactor。</summary>
	public static bool overflowResidualDynamicScaling = true;
	/// <summary>L 缩放参考：scaleL = L/(L+refL)。</summary>
	public static float overflowResidualLactationRefL = 1f;
	/// <summary>炎症倍率：multI = 1 + 本值×Clamp01(I/I_crit)。</summary>
	public static float overflowResidualInflammationBoost = 0.5f;

	// 压力因子模型（模块 2.1）
	public static bool enablePressureFactor = true;
	public static float pressureFactorPc = 0.9f;
	public static float pressureFactorB = 6f;
	public static float pressureFactorMin = 0.02f;

	// 乳汁射出反射模型
	public static bool enableLetdownReflex = true;
	public static float letdownReflexDecayLambda = 0.03f;
	public static float letdownReflexStimulusDeltaR = 0.45f;
	public static float letdownReflexBoostMultiplier = 2f;

	// 炎症模型
	public static bool enableInflammationModel = true;
	public static float inflammationAlpha = 2.2f;
	public static float inflammationBeta = 0.15f;
	public static float inflammationGamma = 0.2f;
	public static float inflammationRho = 0.05f;
	public static float inflammationCrit = 1f;
	public static float inflammationStasisFullnessThreshold = 0.85f;
	public static float inflammationStasisExponent = 2f;
	public static float inflammationHygieneBaselineFactor = 0.2f;
	public static float inflammationDrainReliefScale = 0.35f;
	public static float inflammationDrainReliefMaxPerEvent = 0.14f;
	public static float milkingLStimulusPerEvent = 0.03f;
	public static float milkingLStimulusCapPerEvent = 0.05f;
	public static float milkingLStimulusCapPerDay = 0.2f;

	// 驱动/组织适应模型
	public static bool enableHormoneSaturation = true;
	public static float hormoneSaturationA = 1f;
	public static float hormoneSaturationLRef = 1f;
	public static bool enableTissueAdaptation = true;
	public static float adaptationTheta = 0.002f;
	public static float adaptationOmega = 0.001f;
	public static float adaptationCapMaxRatio = 0.2f;
	public static float adaptationSlowTheta = 0.0008f;
	public static float adaptationSlowOmega = 0.00025f;

	// 事件进水/导管模型
	public static int inflowEventSubsteps = 6;
	public static int inflowEventBurstDurationTicks = 300;
	/// <summary>
	/// 进水哲学：开启=「单一泌乳轴」—小人级 60tick 总预算在各乳池键间按通道权重分配；关闭=「并行独立侧」—每侧独立乘满同一份标度（历史行为，多侧总进水更高）。UI 文案键 EM.InflowSharedMammaryBudget / Desc。
	/// </summary>
	public static bool inflowSharedMammaryBudget;
	public static float ductHopPenaltyPerEdge = 0.15f;
	public static float ductInflowInflammationResistance = 1.5f;
	public static float ductDrainInflammationResistanceManual = 1.2f;
	public static float ductDrainInflammationResistanceMachine = 0.8f;
	public static float ductMachineSuctionBonus = 1.15f;
	public static float ductDrainPressureBase = 0.35f;
	public static float ductDrainPressureScale = 0.65f;
	public static float ductConductanceMin = 0.05f;
	public static float ductConductanceMax = 1.5f;

	// 品质与提醒
	public static bool enableMilkQuality = false;
	public static float milkQualityInflammationWeight = 0.5f;
	public static float milkQualityProtectionFactor = 0.5f;
	public static bool enableFullPoolLetter = true;
	public static float fullPoolLetterCooldownDays = 2f;

	public static float GetPressureFactor(float P)
	{
		if (!enablePressureFactor || P <= 0f) return 1f;
		float k = Mathf.Clamp(pressureFactorB, 0.5f, 80f);
		float pc = Mathf.Clamp(pressureFactorPc, 0.3f, 1f);
		float fMin = Mathf.Clamp01(pressureFactorMin);
		float logistic = 1f / (1f + Mathf.Exp(k * (P - pc)));
		return fMin + (1f - fMin) * logistic;
	}

	public static float GetEffectiveOverflowResidualPressure(float lactationL, float inflammationI)
	{
		float r = Mathf.Clamp01(overflowResidualFlowFactor);
		if (r <= 0f) return 0f;
		if (!overflowResidualDynamicScaling) return r;
		float lRef = Mathf.Max(0.01f, overflowResidualLactationRefL);
		float scaleL = lactationL <= 0f ? 0f : lactationL / (lactationL + lRef);
		float crit = Mathf.Max(0.01f, inflammationCrit);
		float boost = Mathf.Clamp(overflowResidualInflammationBoost, 0f, 3f);
		float scaleI = 1f + boost * Mathf.Clamp01(inflammationI / crit);
		return Mathf.Clamp01(r * scaleL * scaleI);
	}

	public static void ApplyOverflowResidualFlow(ref float pressureFactor, float currentFullness, float stretchCap, float lactationL, float inflammationI)
	{
		float rEff = GetEffectiveOverflowResidualPressure(lactationL, inflammationI);
		if (rEff <= 0f || stretchCap < 0.001f) return;
		if (currentFullness >= stretchCap * PoolModelConstants.FullnessThresholdFactor)
			pressureFactor = Mathf.Max(pressureFactor, rEff);
	}

	public static float GetEffectiveDrive(float L)
	{
		if (!enableHormoneSaturation || L <= 0f) return L;
		float a = Mathf.Clamp(hormoneSaturationA, 0.1f, 3f);
		float h = 1f - Mathf.Exp(-a * L);
		float lRef = Mathf.Max(0f, hormoneSaturationLRef);
		if (lRef >= 0.01f)
		{
			float denom = 1f - Mathf.Exp(-a * lRef);
			if (denom >= 1e-6f) h /= denom;
		}
		return L * h;
	}

	private static void ExposeModelData()
	{
		Scribe_Values.Look(ref enablePressureFactor, "MC2.EM.EnablePressureFactor", true);
		Scribe_Values.Look(ref pressureFactorPc, "MC2.EM.PressureFactorPc", 0.9f);
		Scribe_Values.Look(ref pressureFactorB, "MC2.EM.PressureFactorB", 6f);
		Scribe_Values.Look(ref pressureFactorMin, "MC2.EM.PressureFactorMin", 0.02f);
		Scribe_Values.Look(ref overflowResidualFlowFactor, "MC2.EM.OverflowResidualFlowFactor", 0.04f);
		Scribe_Values.Look(ref overflowResidualDynamicScaling, "MC2.EM.OverflowResidualDynamicScaling", true);
		Scribe_Values.Look(ref overflowResidualLactationRefL, "MC2.EM.OverflowResidualLactationRefL", 1f);
		Scribe_Values.Look(ref overflowResidualInflammationBoost, "MC2.EM.OverflowResidualInflammationBoost", 0.5f);

		Scribe_Values.Look(ref enableLetdownReflex, "MC2.EM.EnableLetdownReflex", true);
		Scribe_Values.Look(ref letdownReflexDecayLambda, "MC2.EM.LetdownReflexDecayLambda", 0.03f);
		Scribe_Values.Look(ref letdownReflexStimulusDeltaR, "MC2.EM.LetdownReflexStimulusDeltaR", 0.45f);
		Scribe_Values.Look(ref letdownReflexBoostMultiplier, "MC2.EM.LetdownReflexBoostMultiplier", 2f);

		Scribe_Values.Look(ref enableInflammationModel, "MC2.EM.EnableInflammationModel", true);
		Scribe_Values.Look(ref inflammationAlpha, "MC2.EM.InflammationAlpha", 2.2f);
		Scribe_Values.Look(ref inflammationBeta, "MC2.EM.InflammationBeta", 0.15f);
		Scribe_Values.Look(ref inflammationGamma, "MC2.EM.InflammationGamma", 0.2f);
		Scribe_Values.Look(ref inflammationRho, "MC2.EM.InflammationRho", 0.05f);
		Scribe_Values.Look(ref inflammationCrit, "MC2.EM.InflammationCrit", 1f);
		Scribe_Values.Look(ref inflammationStasisFullnessThreshold, "MC2.EM.InflammationStasisFullnessThreshold", 0.85f);
		Scribe_Values.Look(ref inflammationStasisExponent, "MC2.EM.InflammationStasisExponent", 2f);
		Scribe_Values.Look(ref inflammationHygieneBaselineFactor, "MC2.EM.InflammationHygieneBaselineFactor", 0.2f);
		Scribe_Values.Look(ref inflammationDrainReliefScale, "MC2.EM.InflammationDrainReliefScale", 0.35f);
		Scribe_Values.Look(ref inflammationDrainReliefMaxPerEvent, "MC2.EM.InflammationDrainReliefMaxPerEvent", 0.14f);

		Scribe_Values.Look(ref milkingLStimulusPerEvent, "MC2.EM.MilkingLStimulusPerEvent", 0.03f);
		Scribe_Values.Look(ref milkingLStimulusCapPerEvent, "MC2.EM.MilkingLStimulusCapPerEvent", 0.05f);
		Scribe_Values.Look(ref milkingLStimulusCapPerDay, "MC2.EM.MilkingLStimulusCapPerDay", 0.2f);
		Scribe_Values.Look(ref enableHormoneSaturation, "MC2.EM.EnableHormoneSaturation", true);
		Scribe_Values.Look(ref hormoneSaturationA, "MC2.EM.HormoneSaturationA", 1f);
		Scribe_Values.Look(ref hormoneSaturationLRef, "MC2.EM.HormoneSaturationLRef", 1f);
		Scribe_Values.Look(ref enableTissueAdaptation, "MC2.EM.EnableTissueAdaptation", true);
		Scribe_Values.Look(ref adaptationTheta, "MC2.EM.AdaptationTheta", 0.002f);
		Scribe_Values.Look(ref adaptationOmega, "MC2.EM.AdaptationOmega", 0.001f);
		Scribe_Values.Look(ref adaptationCapMaxRatio, "MC2.EM.AdaptationCapMaxRatio", 0.2f);
		Scribe_Values.Look(ref adaptationSlowTheta, "MC2.EM.AdaptationSlowTheta", 0.0008f);
		Scribe_Values.Look(ref adaptationSlowOmega, "MC2.EM.AdaptationSlowOmega", 0.00025f);
		Scribe_Values.Look(ref inflowEventSubsteps, "MC2.EM.InflowEventSubsteps", 6);
		Scribe_Values.Look(ref inflowEventBurstDurationTicks, "MC2.EM.InflowEventBurstDurationTicks", 300);
		Scribe_Values.Look(ref inflowSharedMammaryBudget, "MC2.EM.InflowSharedMammaryBudget", false);
		Scribe_Values.Look(ref ductHopPenaltyPerEdge, "MC2.EM.DuctHopPenaltyPerEdge", 0.15f);
		Scribe_Values.Look(ref ductInflowInflammationResistance, "MC2.EM.DuctInflowInflammationResistance", 1.5f);
		Scribe_Values.Look(ref ductDrainInflammationResistanceManual, "MC2.EM.DuctDrainInflammationResistanceManual", 1.2f);
		Scribe_Values.Look(ref ductDrainInflammationResistanceMachine, "MC2.EM.DuctDrainInflammationResistanceMachine", 0.8f);
		Scribe_Values.Look(ref ductMachineSuctionBonus, "MC2.EM.DuctMachineSuctionBonus", 1.15f);
		Scribe_Values.Look(ref ductDrainPressureBase, "MC2.EM.DuctDrainPressureBase", 0.35f);
		Scribe_Values.Look(ref ductDrainPressureScale, "MC2.EM.DuctDrainPressureScale", 0.65f);
		Scribe_Values.Look(ref ductConductanceMin, "MC2.EM.DuctConductanceMin", 0.05f);
		Scribe_Values.Look(ref ductConductanceMax, "MC2.EM.DuctConductanceMax", 1.5f);
		Scribe_Values.Look(ref enableMilkQuality, "MC2.EM.EnableMilkQuality", false);
		Scribe_Values.Look(ref milkQualityInflammationWeight, "MC2.EM.MilkQualityInflammationWeight", 0.5f);
		Scribe_Values.Look(ref milkQualityProtectionFactor, "MC2.EM.MilkQualityProtectionFactor", 0.5f);
		Scribe_Values.Look(ref enableFullPoolLetter, "MC2.EM.EnableFullPoolLetter", true);
		Scribe_Values.Look(ref fullPoolLetterCooldownDays, "MC2.EM.FullPoolLetterCooldownDays", 2f);
	}
}
