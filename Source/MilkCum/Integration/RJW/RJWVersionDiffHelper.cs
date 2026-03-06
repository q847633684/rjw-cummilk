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
}