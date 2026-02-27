using RimWorld;
using Verse;

namespace MilkCum.Milk.Helpers;

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
        if (pawn.IsOnHoldingPlatform)
        {
            return PawnCategory.Entity;
        }
        return PawnCategory.None;
    }
}