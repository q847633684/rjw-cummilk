using Verse;

namespace MilkCum.Core.Settings;

/// <summary>`MilkCumSettings` 的风险与耐受配置分块与序列化。</summary>
internal partial class MilkCumSettings
{
	// Mastitis / hygiene risk grouped settings.
	private static MilkRiskSettings _risk = new MilkRiskSettings();
	private static MilkRiskSettings Risk => _risk;

	public static bool allowMastitis { get => Risk.allowMastitis; set => Risk.allowMastitis = value; }
	public static float mastitisBaseMtbDays { get => Risk.mastitisBaseMtbDays; set => Risk.mastitisBaseMtbDays = value; }
	public static float overFullnessRiskMultiplier { get => Risk.overFullnessRiskMultiplier; set => Risk.overFullnessRiskMultiplier = value; }
	public static float hygieneRiskMultiplier { get => Risk.hygieneRiskMultiplier; set => Risk.hygieneRiskMultiplier = value; }
	public static float mastitisInfectionRiskFactor { get => Risk.mastitisInfectionRiskFactor; set => Risk.mastitisInfectionRiskFactor = value; }
	public static float mastitisMtbDaysMultiplierHumanlike { get => Risk.mastitisMtbDaysMultiplierHumanlike; set => Risk.mastitisMtbDaysMultiplierHumanlike = value; }
	public static float mastitisMtbDaysMultiplierAnimal { get => Risk.mastitisMtbDaysMultiplierAnimal; set => Risk.mastitisMtbDaysMultiplierAnimal = value; }

	private static void ExposeRiskData()
	{
		Scribe_Deep.Look(ref _risk, "EM.MilkRiskSettings");
	}
}
