#if v1_5
using rjw.Modules.Interactions.Extensions;
#endif
using System.Collections.Generic;
using System.Linq;
using rjw;
using rjw.Modules.Interactions;
using Verse;

namespace MilkCum.RJW;

public static class RJWVersionDiffHelper
{
    public static IEnumerable<ISexPartHediff> GetBreasts(this Pawn pawn)
    {
#if v1_5
        return pawn.GetSexablePawnParts().Breasts;
#else
        return pawn.GetLewdParts().Breasts.Select(b => b.SexPart);
#endif
    }

    /// <summary>空安全：pawn 为 null 或 GetBreasts 异常/返回 null 时返回空序列，便于链式调用。</summary>
    public static IEnumerable<ISexPartHediff> GetBreastsOrEmpty(this Pawn pawn)
    {
        if (pawn == null) return Enumerable.Empty<ISexPartHediff>();
        try
        {
            var b = pawn.GetBreasts();
            return b ?? Enumerable.Empty<ISexPartHediff>();
        }
        catch
        {
            return Enumerable.Empty<ISexPartHediff>();
        }
    }

    /// <summary>空安全：pawn 为 null 或 GetBreastList 异常/返回 null 时返回空列表，便于链式调用。</summary>
    public static List<Hediff> GetBreastListOrEmpty(this Pawn pawn)
    {
        if (pawn == null) return new List<Hediff>();
        try
        {
            var list = pawn.GetBreastList();
            return list ?? new List<Hediff>();
        }
        catch
        {
            return new List<Hediff>();
        }
    }
}