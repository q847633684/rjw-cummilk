using RimWorld;
using Verse;

namespace EqualMilking
{
    /// <summary>10.8-5：当成瘾处于满足阶段时显示小幅正面心情。</summary>
    public class ThoughtWorker_AddictionSatisfied : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            var addiction = p?.health?.hediffSet?.GetFirstHediffOfDef(EMDefOf.EM_Prolactin_Addiction);
            if (addiction == null) return ThoughtState.Inactive;
            if (addiction.CurStageIndex != 0) return ThoughtState.Inactive; // 0 = 满足
            return ThoughtState.ActiveAtStage(0);
        }
    }
}
