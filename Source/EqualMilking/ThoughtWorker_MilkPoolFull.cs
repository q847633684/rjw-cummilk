using RimWorld;
using Verse;
using EqualMilking.Helpers;

namespace EqualMilking
{
    /// <summary>10.8-6：满池时（人类）显示轻微不适心情。</summary>
    public class ThoughtWorker_MilkPoolFull : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p?.RaceProps?.Humanlike != true) return ThoughtState.Inactive;
            var comp = p.CompEquallyMilkable();
            if (comp == null || !p.IsLactating()) return ThoughtState.Inactive;
            if (comp.Fullness < 0.95f) return ThoughtState.Inactive;
            return ThoughtState.ActiveAtStage(0);
        }
    }
}
