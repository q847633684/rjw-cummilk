using RimWorld;
using Verse;

namespace MilkCum.UI;
/// <summary>奶池满度列：无本 mod 泌乳 Comp 时显示 "-"（如其他 mod 的 Lactating）。</summary>
public class PawnColumnWorker_MilkFullness : PawnColumnWorker_Text
{
	protected override string GetTextFor(Pawn pawn)
	{
		if (!pawn.IsLactating()) { return "-"; }
		var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating);
		var comp = hediff?.TryGetComp<HediffComp_EqualMilkingLactating>();
		return comp == null ? "-" : comp.Charge.ToStringPercent();
	}
	protected override string GetHeaderTip(PawnTable table)
	{
		return "EM.MilkFullnessColumnTip".Translate();
	}
}