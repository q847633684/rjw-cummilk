using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 同 tick、同 <see cref="RjwBreastPoolEconomy.BuildBreastListPoolKeySignature"/> 下复用 <see cref="RjwBreastPoolEconomy.GetBreastPoolSideRows"/> 结果，避免一帧内多次遍历乳房列表与拼路径键。
/// 列表视为只读；勿对返回值执行 Add/Remove（当前调用方均为读）。键签名变则下一请求重建。
/// </summary>
internal static class BreastPoolSideRowsCache
{
    private sealed class Entry
    {
        public int Tick = -1;
        public string Sig = "";
        public List<RjwBreastPoolSideRow> Rows;
    }

    private static readonly ConditionalWeakTable<Pawn, Entry> Table = new();

    public static List<RjwBreastPoolSideRow> GetCached(Pawn pawn, Func<Pawn, List<RjwBreastPoolSideRow>> build)
    {
        if (pawn == null)
        {
            return new List<RjwBreastPoolSideRow>();
        }

        int tick = Find.TickManager?.TicksGame ?? -1;
        string sig = RjwBreastPoolEconomy.BuildBreastListPoolKeySignature(pawn);
        Entry e = Table.GetOrCreateValue(pawn);
        if (e.Rows != null && tick == e.Tick && sig == e.Sig)
        {
            return e.Rows;
        }

        List<RjwBreastPoolSideRow> fresh = build(pawn);
        e.Tick = tick;
        e.Sig = sig;
        e.Rows = fresh;
        return e.Rows;
    }
}
