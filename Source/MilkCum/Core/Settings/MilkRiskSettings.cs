using Verse;

namespace MilkCum.Core.Settings;

/// <summary>乳腺炎与耐受相关的设置分组。</summary>
public class MilkRiskSettings : IExposable
{
	public bool allowMastitis = true;
	public float mastitisBaseMtbDays = 1.5f;
	public float overFullnessRiskMultiplier = 1.5f;
	public float hygieneRiskMultiplier = 1f;
	/// <summary>医学贴近：卫生差+淤积/损伤时 MTB 再除以此值（感染风险）。</summary>
	public float mastitisInfectionRiskFactor = 1.2f;
	/// <summary>人形与动物的乳腺炎 MTB 乘数，用于分开平衡风险。</summary>
	public float mastitisMtbDaysMultiplierHumanlike = 1f;
	public float mastitisMtbDaysMultiplierAnimal = 1f;

	public void ExposeData()
	{
		Scribe_Values.Look(ref allowMastitis, "MC2.EM.AllowMastitis", true);
		Scribe_Values.Look(ref mastitisBaseMtbDays, "MC2.EM.MastitisBaseMtbDays", 1.5f);
		Scribe_Values.Look(ref overFullnessRiskMultiplier, "MC2.EM.OverFullnessRiskMultiplier", 1.5f);
		Scribe_Values.Look(ref hygieneRiskMultiplier, "MC2.EM.HygieneRiskMultiplier", 1f);
		Scribe_Values.Look(ref mastitisInfectionRiskFactor, "MC2.EM.MastitisInfectionRiskFactor", 1.2f);
		Scribe_Values.Look(ref mastitisMtbDaysMultiplierHumanlike, "MC2.EM.MastitisMtbDaysMultiplierHumanlike", 1f);
		Scribe_Values.Look(ref mastitisMtbDaysMultiplierAnimal, "MC2.EM.MastitisMtbDaysMultiplierAnimal", 1f);
	}
}
