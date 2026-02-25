using System.Collections.Generic;
using Verse;

namespace Cumpilation.Gathering
{
    /// <summary>
    /// 本 Mod 生成污物时登记产主；高级桶吸收时可按床主过滤，并给产出打产主。
    /// 键为 (地图, 格, 污物Def)，同一格多次生成时后者覆盖（仅保留最后产主）。
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

        /// <summary>取出并移除该格该污物的产主记录；无记录则返回 null。</summary>
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
