using Verse;
using RimWorld;

namespace EqualMilking.Helpers
{
    /// <summary>可选联动：Dubs Bad Hygiene。当 DBH 已加载时，乳腺炎/堵塞的「卫生」触发可基于 DBH 的 Hygiene 需求；否则使用房间清洁度。</summary>
    public static class DubsBadHygieneIntegration
    {
        private static NeedDef _cachedHygieneNeedDef;
        private static bool _cachedHygieneNeedChecked;

        /// <summary>Dubs Bad Hygiene 是否已加载（存在 Hygiene NeedDef）。</summary>
        public static bool IsDubsBadHygieneActive()
        {
            if (_cachedHygieneNeedChecked) return _cachedHygieneNeedDef != null;
            _cachedHygieneNeedChecked = true;
            _cachedHygieneNeedDef = DefDatabase<NeedDef>.GetNamedSilentFail("Hygiene");
            return _cachedHygieneNeedDef != null;
        }

        /// <summary>卫生风险系数 0~1（1=最易诱发乳腺炎）。当启用 DBH 联动时用 Hygiene 需求等级（低=高风险）；否则用当前房间清洁度（脏=高风险）。</summary>
        public static float GetHygieneRiskFactorForMastitis(Pawn pawn)
        {
            if (pawn?.needs == null) return 0f;
            if (IsDubsBadHygieneActive() && EqualMilkingSettings.useDubsBadHygieneForMastitis)
            {
                var need = pawn.needs.TryGetNeed(_cachedHygieneNeedDef);
                if (need != null)
                    return 1f - need.CurLevelPercentage; // 低卫生 = 高风险
                return 0.5f;
            }
            var room = pawn.GetRoom();
            if (room == null) return 0f;
            var cleanlinessDef = DefDatabase<RoomStatDef>.GetNamedSilentFail("Cleanliness");
            if (cleanlinessDef == null) return 0f;
            float cleanliness = room.GetStat(cleanlinessDef);
            if (cleanliness >= 0f) return 0f;
            return Mathf.Clamp01(-cleanliness);
        }
    }
}
