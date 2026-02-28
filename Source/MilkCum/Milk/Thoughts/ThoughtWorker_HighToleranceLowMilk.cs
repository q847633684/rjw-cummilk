using RimWorld;
using Verse;
using MilkCum.Core;
using MilkCum.Milk.Helpers;
using MilkCum.Milk.Comps;

namespace MilkCum.Milk.Thoughts;

/// <summary>建议 9：高耐受且当前泌乳量(L)偏低时，产生轻微挫败感心情。</summary>
public class ThoughtWorker_HighToleranceLowMilk : ThoughtWorker
{
	private const float ToleranceThreshold = 0.5f;
	private const float LowLThreshold = 0.35f;

	protected override ThoughtState CurrentStateInternal(Pawn p)
	{
		if (p?.RaceProps?.Humanlike != true) return ThoughtState.Inactive;
		if (!p.IsLactating()) return ThoughtState.Inactive;
		var hediff = p.LactatingHediffWithComps();
		var comp = hediff?.comps?.Find(c => c is HediffComp_EqualMilkingLactating) as HediffComp_EqualMilkingLactating;
		if (comp == null) return ThoughtState.Inactive;
		float tolerance = EqualMilkingSettings.GetProlactinTolerance(p);
		if (tolerance < ToleranceThreshold) return ThoughtState.Inactive;
		if (comp.CurrentLactationAmount > LowLThreshold) return ThoughtState.Inactive;
		return ThoughtState.ActiveAtStage(0);
	}
}
