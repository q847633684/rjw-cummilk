using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MilkCum.Fluids.Cum.Leaking
{
    public class ThoughtWorker_WearingPlug : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.apparel.WornApparel.Any(a => a.def == DefOfs.Cumpilation_Apparel_Plug))
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.Inactive;
        }
    }
}