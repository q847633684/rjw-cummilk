using Verse;

namespace MilkCum.Core;

/// <summary>建议 22：关键默认值 Def，供其他 mod 通过 PatchOperation 覆盖；可用「从 Def 加载默认」按钮应用。</summary>
public class EqualMilkingDefaultsDef : Def
{
	// 基准泌乳天数（池模型 B_T / B_T_birth）
	public float baselineMilkDurationDays = 5f;
	public float birthInducedMilkDurationDays = 10f;
	/// <summary>流速倍率；默认 2 = 单次剂量约 1 日灌满。</summary>
	public float defaultFlowMultiplierForHumanlike = 2f;
	public bool allowMastitis = true;
	public float mastitisBaseMtbDays = 1.5f;
	public float overFullnessRiskMultiplier = 1.5f;
	public float hygieneRiskMultiplier = 1f;
	// 耐受
	public bool allowToleranceAffectMilk = true;
	public float toleranceFlowImpactExponent = 1f;
	// 溢出与 AI
	public string overflowFilthDefName = "Filth_Vomit";
	public bool aiPreferHighFullnessTargets = true;
}
