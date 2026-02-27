using Verse;
using Verse.AI;
using Cumpilation.Cumflation;
using MilkCum.Milk.Helpers;

namespace Cumpilation.Leaking
{
    public class ThinkNode_ConditionalCumflationSeverity : ThinkNode_Conditional
    {
        public Hediff cumflationHediff;

        protected override bool Satisfied(Pawn pawn)
        {
            cumflationHediff = CumflationUtility.GetOrCreateCumflationHediff(pawn);
            if (cumflationHediff == null)
            {
                return false;
            }
            // 若奶非常满而 Cumflation 只是略高于阈值，则优先让挤奶 Job 抢人，暂缓自动泄精
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