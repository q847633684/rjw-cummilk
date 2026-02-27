using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MilkCum.UI;
public class Milk_PawnTable : PawnTable_PlayerPawns
{
    public Milk_PawnTable(PawnTableDef def, Func<IEnumerable<Pawn>> pawnsGetter, int uiWidth, int uiHeight) : base(def, pawnsGetter, uiWidth, uiHeight) { }

    protected override IEnumerable<Pawn> LabelSortFunction(IEnumerable<Pawn> input)
    {
        return input.OrderByDescending(p => p.RaceProps.Humanlike).ThenByDescending(p => p.IsFreeNonSlaveColonist).ThenByDescending(p => p.IsPrisonerOfColony).ThenByDescending(p => p.IsSlaveOfColony).ThenByDescending(p => p.RaceProps.Animal).ThenBy(p => p.LabelCap);
    }
}
