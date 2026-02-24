using RimWorld;
using rjw;
using Verse;

namespace Milk
{
    //ThoughtWorker_Pain
    public class ThoughtWorker_MilkFullness : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            //if (p.story.traits.HasTrait(TraitDefOf.Masochist))
            //    return ThoughtState.Inactive;

            var comp = ThingCompUtility.TryGetComp<CompMilkableHuman>(p);
            if (comp != null)
            {
                if (comp.Fullness>0 && comp.Fullness <0.5) 
                    return ThoughtState.ActiveAtStage(0);
                if (comp.Fullness >= 0.5 && comp.Fullness < 1)
                    return ThoughtState.ActiveAtStage(1);
                if (comp.Fullness >= 1 && comp.Fullness < 1.4)
                    return ThoughtState.ActiveAtStage(2);
                if (comp.Fullness >= 1.4)
                    return ThoughtState.ActiveAtStage(3);

            }
            return ThoughtState.Inactive;

        }
    }
}