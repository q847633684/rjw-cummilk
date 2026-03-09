using Verse;

namespace MilkCum.Core.Settings;

/// <summary>建议 22：关键默认值 Def，供「从 Def 加载默认」使用；若 XML 未加载则用 GetBuiltinDefaults()。其他 mod 可 patch GetBuiltinDefaults 改内置值。</summary>
public class MilkCumDefaultsDef : Def
{
	// 药物诱发：基准泌乳天数；分娩诱发：参考天数（当前池衰减未用）
	public float baselineMilkDurationDays = 5f;
	public float birthInducedMilkDurationDays = 30f;
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

	/// <summary>内置默认值（当 XML 未加载时使用；其他 mod 可 patch 此方法修改）。</summary>
	public static MilkCumDefaultsDef GetBuiltinDefaults()
	{
		return new MilkCumDefaultsDef
		{
			defName = "EM_Defaults",
			label = "EM.DefaultsDefLabel",
			baselineMilkDurationDays = 5f,
			birthInducedMilkDurationDays = 30f,
			defaultFlowMultiplierForHumanlike = 2f,
			allowMastitis = true,
			mastitisBaseMtbDays = 1.5f,
			overFullnessRiskMultiplier = 1.5f,
			hygieneRiskMultiplier = 1f,
			allowToleranceAffectMilk = true,
			toleranceFlowImpactExponent = 1f,
			overflowFilthDefName = "Filth_Vomit",
			aiPreferHighFullnessTargets = true
		};
	}
}
