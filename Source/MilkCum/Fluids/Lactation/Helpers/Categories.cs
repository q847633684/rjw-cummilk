using RimWorld;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

public static class Categories
{
    public enum PawnCategory
    {
        None,
        Colonist,
        Prisoner,
        Slave,
        Animal,
        Mechanoid,
        Entity
    }
    public static string Label(this PawnCategory category)
    {
        return category switch
        {
            PawnCategory.Colonist => Lang.Colonist.CapitalizeFirst(),
            PawnCategory.Prisoner => Lang.Prisoner.CapitalizeFirst(),
            PawnCategory.Slave => Lang.Slave.CapitalizeFirst(),
            PawnCategory.Animal => Lang.Animal.CapitalizeFirst(),
            PawnCategory.Mechanoid => Lang.Mechanoid.CapitalizeFirst(),
            PawnCategory.Entity => Lang.Entities.CapitalizeFirst(),
            _ => "None"
        };
    }
    public static PawnCategory GetPawnCategory(Pawn pawn)
    {
        if (pawn == null) { return PawnCategory.None; }
        // Anomaly 实体 / 收容平台：与上游一致，优先归类为 Entity，避免殖民者、亚人类等分流抢走类别（挤奶默认设置、WorkGiver 等）。
        if (pawn.IsEntity || pawn.IsOnHoldingPlatform)
            return PawnCategory.Entity;
        if (pawn.IsColonist && !pawn.IsPrisonerOfColony && !pawn.IsSlaveOfColony)
        {
            return PawnCategory.Colonist;
        }
        if (pawn.IsPrisonerOfColony)
        {
            return PawnCategory.Prisoner;
        }
        if (pawn.IsSlaveOfColony)
        {
            return PawnCategory.Slave;
        }
        if (pawn.RaceProps.Animal && pawn.Faction.IsPlayerSafe())
        {
            return PawnCategory.Animal;
        }
        if (pawn.IsColonyMech)
        {
            return PawnCategory.Mechanoid;
        }
        return PawnCategory.None;
    }
}