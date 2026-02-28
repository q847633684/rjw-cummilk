using RimWorld;
using Verse;
using MilkCum.Milk.Helpers;
using MilkCum.Milk.Comps;

namespace MilkCum.UI;

/// <summary>建议 7：奶表格中显示泌乳剩余天数。</summary>
public class PawnColumnWorker_MilkRemainingDays : PawnColumnWorker_Text
{
	protected override string GetTextFor(Pawn pawn)
	{
		var hediff = pawn?.LactatingHediffWithComps();
		var comp = hediff?.comps?.Find(c => c is HediffComp_EqualMilkingLactating) as HediffComp_EqualMilkingLactating;
		if (comp == null || comp.RemainingDays <= 0f) return "-";
		return comp.RemainingDays.ToString("F1") + "d";
	}
}
