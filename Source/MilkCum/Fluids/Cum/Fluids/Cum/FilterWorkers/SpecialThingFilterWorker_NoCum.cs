using Verse;

namespace MilkCum.Fluids.Cum.Fluids.Cum
{
	public class SpecialThingFilterWorker_NoCum : SpecialThingFilterWorker_CumBase
	{
		public override bool Matches(Thing t)
		{
			return !IsCum(t.def) && !IsFoodWithCum(t);
		}
	}
}
