using Verse;
using Verse.AI;
using MilkCum.Fluids.Cum;
using MilkCum.Fluids.Cum.Cumflation;

namespace MilkCum.Fluids.Cum.Leaking
{
    public class ThinkNode_ConditionalCumflationSeverity : ThinkNode_Conditional
    {
        public Hediff cumflationHediff;

        protected override bool Satisfied(Pawn pawn)
        {
            cumflationHediff = MenstruationFluidsCompat.TryGetActiveCumflationForJobs(pawn);
            if (cumflationHediff == null)
            {
                return false;
            }
            // 奶池非常满且 Cumflation 仅轻中度时，优先挤奶而非自动泄精。
            var comp = pawn.CompEquallyMilkable();
            if (comp != null && comp.Fullness >= comp.maxFullness * 0.9f && cumflationHediff.Severity < 1.0f)
            {
                return false;
            }
            return cumflationHediff.Severity > Settings.AutoDeflateMinSeverity;
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalCumflationSeverity obj = (ThinkNode_ConditionalCumflationSeverity)base.DeepCopy(resolve);
            obj.cumflationHediff = cumflationHediff;
            return obj;
        }
    }
}