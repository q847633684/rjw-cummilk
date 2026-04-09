using System.Collections.Generic;
using Verse;

namespace MilkCum.Fluids.Cum.Gathering
{
    /// <summary>
    /// 记录本 mod 生成污物时的产主。高级桶吸收污物时可按床主过滤，并把产主写回产出物。
    /// 键为 (地图, 格子, 污物Def)。同格同 Def 多次记录时以后写覆盖先写。
    /// </summary>
    public static class FilthProducerRegistry
    {
        private static readonly Dictionary<(Map map, IntVec3 cell, ThingDef filthDef), Pawn> _registry = new();

        public static void Record(Map map, IntVec3 cell, ThingDef filthDef, Pawn producer)
        {
            if (map == null || filthDef == null) return;
            var key = (map, cell, filthDef);
            if (producer != null)
                _registry[key] = producer;
            else
                _registry.Remove(key);
        }

        /// <summary>取出并移除该格该污物的产主记录；无记录返回 null。</summary>
        public static Pawn GetAndRemove(Map map, IntVec3 cell, ThingDef filthDef)
        {
            if (map == null || filthDef == null) return null;
            var key = (map, cell, filthDef);
            if (_registry.TryGetValue(key, out var pawn))
            {
                _registry.Remove(key);
                return pawn;
            }
            return null;
        }
    }
}
