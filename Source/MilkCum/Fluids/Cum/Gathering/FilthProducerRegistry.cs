using System.Collections.Generic;
using Verse;

namespace MilkCum.Fluids.Cum.Gathering
{
    /// <summary>
    /// 鏈?Mod 鐢熸垚姹＄墿鏃剁櫥璁颁骇涓伙紱楂樼骇妗跺惛鏀舵椂鍙寜搴婁富杩囨护锛屽苟缁欎骇鍑烘墦浜т富銆?
    /// 閿负 (鍦板浘, 鏍? 姹＄墿Def)锛屽悓涓€鏍煎娆＄敓鎴愭椂鍚庤€呰鐩栵紙浠呬繚鐣欐渶鍚庝骇涓伙級銆?
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

        /// <summary>鍙栧嚭骞剁Щ闄よ鏍艰姹＄墿鐨勪骇涓昏褰曪紱鏃犺褰曞垯杩斿洖 null銆</summary>
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
