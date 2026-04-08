using MilkCum.Fluids.Cum.Cumflation;
using Verse;

namespace MilkCum.Fluids.Cum;

/// <summary>
/// 与 RJW Menstruation Fluids 的 Def 级逻辑：判断是否跳过 TransferFluids 充盈、泄殖作业使用哪类充盈 Hediff。
/// 程序集级联动见 <see cref="MilkCum.Fluids.Cum.Common.CreateFluidList"/> 等（硬前置 MF）。
/// </summary>
[StaticConstructorOnStartup]
public static class MenstruationFluidsCompat
{
	static HediffDef mfVaginal;
	static HediffDef mfAnal;

	static MenstruationFluidsCompat()
	{
		mfVaginal = DefDatabase<HediffDef>.GetNamedSilentFail("MenstruationFluids_Cumflation_Vaginal");
		mfAnal = DefDatabase<HediffDef>.GetNamedSilentFail("MenstruationFluids_Cumflation_Anal");
	}

	public static HediffDef MenstruationCumflationVaginal => mfVaginal;

	public static HediffDef MenstruationCumflationAnal => mfAnal;

	/// <summary>当前 Def 数据库中存在 MF 充盈 Hediff 定义。</summary>
	public static bool MenstruationFluidsCumflationDefsPresent => mfVaginal != null || mfAnal != null;

	public static bool PawnHasAnyDeflatableCumflation(Pawn p)
	{
		if (p?.health?.hediffSet == null)
			return false;
		if (p.health.hediffSet.HasHediff(DefOfs.Cumpilation_Cumflation))
			return true;
		if (mfVaginal != null && p.health.hediffSet.HasHediff(mfVaginal))
			return true;
		if (mfAnal != null && p.health.hediffSet.HasHediff(mfAnal))
			return true;
		return false;
	}

	/// <summary>
	/// 自动泄殖/进度条等使用的「当前充盈 Hediff」：优先 MF 阴道/肛门，否则回退 Cumpilation_Cumflation（旧档）。
	/// </summary>
	public static Hediff GetActiveCumflationForJobs(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
			return null;

		if (!MenstruationFluidsCumflationDefsPresent)
			return CumflationUtility.GetOrCreateCumflationHediff(pawn);

		Hediff vaginal = mfVaginal != null ? pawn.health.hediffSet.GetFirstHediffOfDef(mfVaginal) : null;
		Hediff anal = mfAnal != null ? pawn.health.hediffSet.GetFirstHediffOfDef(mfAnal) : null;
		Hediff highest = vaginal;
		if (anal != null && (highest == null || anal.Severity > highest.Severity))
			highest = anal;
		if (highest != null)
			return highest;

		Hediff legacy = pawn.health.hediffSet.GetFirstHediffOfDef(DefOfs.Cumpilation_Cumflation);
		if (legacy != null)
			return legacy;

		HediffDef def = mfVaginal ?? mfAnal;
		if (def == null)
			return CumflationUtility.GetOrCreateCumflationHediff(pawn);

		highest = HediffMaker.MakeHediff(def, pawn);
		highest.Severity = 0.01f;
		return highest;
	}
}
