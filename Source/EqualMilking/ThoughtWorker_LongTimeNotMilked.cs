using RimWorld;
using Verse;
using EqualMilking.Helpers;

namespace EqualMilking
{
    /// <summary>10.8-6：人类泌乳且奶量较多但超过 2 游戏日未被挤奶时显示不适心情。</summary>
    public class ThoughtWorker_LongTimeNotMilked : ThoughtWorker
    {
        private const int TicksPerDay = 60000;
        private const int DaysThreshold = 2;

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p?.RaceProps?.Humanlike != true) return ThoughtState.Inactive;
            var comp = p.CompEquallyMilkable();
            if (comp == null || !p.IsLactating()) return ThoughtState.Inactive;
            if (comp.Fullness < 0.3f) return ThoughtState.Inactive;
            int lastTick = comp.LastGatheredTick;
            if (lastTick < 0) return ThoughtState.Inactive;
            int elapsed = Find.TickManager.TicksGame - lastTick;
            if (elapsed < TicksPerDay * DaysThreshold) return ThoughtState.Inactive;
            return ThoughtState.ActiveAtStage(0);
        }
    }
}
