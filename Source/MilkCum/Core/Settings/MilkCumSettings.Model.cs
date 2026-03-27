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
		Scribe_Values.Look(ref enablePressureFactor, "EM.EnablePressureFactor", true);
		Scribe_Values.Look(ref pressureFactorPc, "EM.PressureFactorPc", 0.9f);
		Scribe_Values.Look(ref pressureFactorB, "EM.PressureFactorB", 6f);
		Scribe_Values.Look(ref pressureFactorMin, "EM.PressureFactorMin", 0.02f);
		Scribe_Values.Look(ref overflowResidualFlowFactor, "EM.OverflowResidualFlowFactor", 0.04f);
		Scribe_Values.Look(ref overflowResidualDynamicScaling, "EM.OverflowResidualDynamicScaling", true);
		Scribe_Values.Look(ref overflowResidualLactationRefL, "EM.OverflowResidualLactationRefL", 1f);
		Scribe_Values.Look(ref overflowResidualInflammationBoost, "EM.OverflowResidualInflammationBoost", 0.5f);

		Scribe_Values.Look(ref enableLetdownReflex, "EM.EnableLetdownReflex", true);
		Scribe_Values.Look(ref letdownReflexDecayLambda, "EM.LetdownReflexDecayLambda", 0.03f);
		Scribe_Values.Look(ref letdownReflexStimulusDeltaR, "EM.LetdownReflexStimulusDeltaR", 0.45f);
		Scribe_Values.Look(ref letdownReflexBoostMultiplier, "EM.LetdownReflexBoostMultiplier", 2f);

		Scribe_Values.Look(ref enableInflammationModel, "EM.EnableInflammationModel", true);
		Scribe_Values.Look(ref inflammationAlpha, "EM.InflammationAlpha", 2.2f);
		Scribe_Values.Look(ref inflammationBeta, "EM.InflammationBeta", 0.15f);
		Scribe_Values.Look(ref inflammationGamma, "EM.InflammationGamma", 0.2f);
		Scribe_Values.Look(ref inflammationRho, "EM.InflammationRho", 0.05f);
		Scribe_Values.Look(ref inflammationCrit, "EM.InflammationCrit", 1f);
		Scribe_Values.Look(ref inflammationStasisFullnessThreshold, "EM.InflammationStasisFullnessThreshold", 0.85f);
		Scribe_Values.Look(ref inflammationStasisExponent, "EM.InflammationStasisExponent", 2f);
		Scribe_Values.Look(ref inflammationHygieneBaselineFactor, "EM.InflammationHygieneBaselineFactor", 0.2f);
		Scribe_Values.Look(ref inflammationDrainReliefScale, "EM.InflammationDrainReliefScale", 0.35f);
		Scribe_Values.Look(ref inflammationDrainReliefMaxPerEvent, "EM.InflammationDrainReliefMaxPerEvent", 0.14f);

		Scribe_Values.Look(ref milkingLStimulusPerEvent, "EM.MilkingLStimulusPerEvent", 0.03f);
		Scribe_Values.Look(ref milkingLStimulusCapPerEvent, "EM.MilkingLStimulusCapPerEvent", 0.05f);
		Scribe_Values.Look(ref milkingLStimulusCapPerDay, "EM.MilkingLStimulusCapPerDay", 0.2f);
		Scribe_Values.Look(ref enableHormoneSaturation, "EM.EnableHormoneSaturation", true);
		Scribe_Values.Look(ref hormoneSaturationA, "EM.HormoneSaturationA", 1f);
		Scribe_Values.Look(ref hormoneSaturationLRef, "EM.HormoneSaturationLRef", 1f);
		Scribe_Values.Look(ref enableTissueAdaptation, "EM.EnableTissueAdaptation", true);
		Scribe_Values.Look(ref adaptationTheta, "EM.AdaptationTheta", 0.002f);
		Scribe_Values.Look(ref adaptationOmega, "EM.AdaptationOmega", 0.001f);
		Scribe_Values.Look(ref adaptationCapMaxRatio, "EM.AdaptationCapMaxRatio", 0.2f);
		Scribe_Values.Look(ref adaptationSlowTheta, "EM.AdaptationSlowTheta", 0.0008f);
		Scribe_Values.Look(ref adaptationSlowOmega, "EM.AdaptationSlowOmega", 0.00025f);
		Scribe_Values.Look(ref inflowEventSubsteps, "EM.InflowEventSubsteps", 6);
		Scribe_Values.Look(ref inflowEventBurstDurationTicks, "EM.InflowEventBurstDurationTicks", 300);
		Scribe_Values.Look(ref ductHopPenaltyPerEdge, "EM.DuctHopPenaltyPerEdge", 0.15f);
		Scribe_Values.Look(ref ductInflowInflammationResistance, "EM.DuctInflowInflammationResistance", 1.5f);
		Scribe_Values.Look(ref ductDrainInflammationResistanceManual, "EM.DuctDrainInflammationResistanceManual", 1.2f);
		Scribe_Values.Look(ref ductDrainInflammationResistanceMachine, "EM.DuctDrainInflammationResistanceMachine", 0.8f);
		Scribe_Values.Look(ref ductMachineSuctionBonus, "EM.DuctMachineSuctionBonus", 1.15f);
		Scribe_Values.Look(ref ductDrainPressureBase, "EM.DuctDrainPressureBase", 0.35f);
		Scribe_Values.Look(ref ductDrainPressureScale, "EM.DuctDrainPressureScale", 0.65f);
		Scribe_Values.Look(ref ductConductanceMin, "EM.DuctConductanceMin", 0.05f);
		Scribe_Values.Look(ref ductConductanceMax, "EM.DuctConductanceMax", 1.5f);
		Scribe_Values.Look(ref enableMilkQuality, "EM.EnableMilkQuality", false);
		Scribe_Values.Look(ref milkQualityInflammationWeight, "EM.MilkQualityInflammationWeight", 0.5f);
		Scribe_Values.Look(ref milkQualityProtectionFactor, "EM.MilkQualityProtectionFactor", 0.5f);
		Scribe_Values.Look(ref enableFullPoolLetter, "EM.EnableFullPoolLetter", true);
		Scribe_Values.Look(ref fullPoolLetterCooldownDays, "EM.FullPoolLetterCooldownDays", 2f);
	}
}
