using RimWorld;
using Verse;

namespace EqualMilking
{
    /// <summary>10.8-4：仅当成瘾处于戒断阶段时显示催乳素戒断心情。</summary>
    public class ThoughtWorker_ProlactinWithdrawal : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            var addiction = p?.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Addiction);
            if (addiction == null) return ThoughtState.Inactive;
            // Hediff_Addiction: CurStageIndex 0 = 满足, 1 = 戒断
            if (addiction.CurStageIndex != 1) return ThoughtState.Inactive;
            return ThoughtState.ActiveAtStage(0);
        }
    }
}
