using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Integration.DubsBadHygiene;
    /// <summary>可选联动：Dubs Bad Hygiene。当 DBH 已加载且存在 Hygiene 需求时，乳腺炎/堵塞的卫生风险始终基于 Hygiene；否则使用房间清洁度。喝奶（吸奶或食用奶瓶）时满足 DBH 的饮水(Thirst)需求。</summary>
    public static class DubsBadHygieneIntegration
    {
        private static NeedDef _cachedHygieneNeedDef;
        private static bool _cachedHygieneNeedChecked;
        private static NeedDef _cachedThirstNeedDef;
        private static bool _cachedThirstNeedChecked;

        /// <summary>Dubs Bad Hygiene 是否已加载（存在 Hygiene NeedDef 或检测到 DBH 模组）。</summary>
        public static bool IsDubsBadHygieneActive()
        {
            if (_cachedHygieneNeedChecked)
                return _cachedHygieneNeedDef != null || IsDubsBadHygieneModPresent();
            _cachedHygieneNeedChecked = true;
            _cachedHygieneNeedDef = DefDatabase<NeedDef>.GetNamedSilentFail("Hygiene")
                ?? DefDatabase<NeedDef>.GetNamedSilentFail("DBH_Hygiene");
            if (_cachedHygieneNeedDef == null && IsDubsBadHygieneModPresent())
            {
                var needDef = DefDatabase<NeedDef>.AllDefs.FirstOrDefault(d =>
                    d.defName != null && d.defName.IndexOf("Hygiene", System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (needDef != null)
                    _cachedHygieneNeedDef = needDef;
            }
            return _cachedHygieneNeedDef != null || IsDubsBadHygieneModPresent();
        }

        /// <summary>饮水(Thirst)需求是否可用（DBH 已加载且启用 Thirst 时）。</summary>
        public static bool IsThirstNeedAvailable()
        {
            if (!IsDubsBadHygieneActive()) return false;
            if (_cachedThirstNeedChecked) return _cachedThirstNeedDef != null;
            _cachedThirstNeedChecked = true;
            _cachedThirstNeedDef = DefDatabase<NeedDef>.GetNamedSilentFail("Thirst")
                ?? DefDatabase<NeedDef>.GetNamedSilentFail("DBH_Thirst");
            if (_cachedThirstNeedDef == null)
            {
                var needDef = DefDatabase<NeedDef>.AllDefs.FirstOrDefault(d =>
                    d.defName != null && d.defName.IndexOf("Thirst", System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (needDef != null)
                    _cachedThirstNeedDef = needDef;
            }
            return _cachedThirstNeedDef != null;
        }

        /// <summary>为喝奶者满足饮水需求。amount 为池单位/营养当量（1 池单位=1 营养），按 1:1 加到 Thirst.CurLevel，上限 MaxLevel。仅当 DBH 已加载且存在 Thirst 需求时生效。</summary>
        public static void SatisfyThirst(Pawn pawn, float amount)
        {
            if (pawn?.needs == null || amount <= 0f || !IsThirstNeedAvailable() || _cachedThirstNeedDef == null)
                return;
            var need = pawn.needs.TryGetNeed(_cachedThirstNeedDef);
            if (need == null) return;
            need.CurLevel = Mathf.Min(need.MaxLevel, need.CurLevel + amount);
        }

        /// <summary>Thirst 需求有则返回 CurLevelPercentage（0~1，高=不渴）；无则 false。</summary>
        public static bool TryGetThirstCurLevel01(Pawn pawn, out float curLevel01)
        {
            curLevel01 = 1f;
            if (pawn?.needs == null || !IsThirstNeedAvailable() || _cachedThirstNeedDef == null)
                return false;
            var need = pawn.needs.TryGetNeed(_cachedThirstNeedDef);
            if (need == null) return false;
            curLevel01 = need.CurLevelPercentage;
            return true;
        }

        private static bool IsDubsBadHygieneModPresent()
        {
            if (ModLister.GetModWithIdentifier("dubwise.dubsbadhygiene") != null)
                return true;
            return ModLister.AllInstalledMods.Any(m =>
                m.Active && m.PackageIdPlayerFacing != null
                && m.PackageIdPlayerFacing.IndexOf("dubs", System.StringComparison.OrdinalIgnoreCase) >= 0
                && m.PackageIdPlayerFacing.IndexOf("hygiene", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>卫生风险系数 0~1（1=最易诱发乳腺炎）。当启用 DBH 联动时用 Hygiene 需求等级（低=高风险）；否则用当前房间清洁度（脏=高风险）。</summary>
        public static float GetHygieneRiskFactorForMastitis(Pawn pawn)
        {
            if (pawn?.needs == null) return 0f;
            if (IsDubsBadHygieneActive() && _cachedHygieneNeedDef != null)
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
