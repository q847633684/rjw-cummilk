using MilkCum.Core;
using RimWorld;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 仅保留非 Pawn 的扩展入口：CompEquallyMilkable（ThingWithComps）、Set（CompProperties_Milkable）�?/// �?Pawn 相关扩展已拆�?PawnMilkStateExtensions、MilkPermissionExtensions、PawnMilkPoolExtensions，见 记忆�?design/架构原则与重组建议�?/// </summary>
public static class ExtensionHelper
{
    public static void Set(this CompProperties_Milkable comp, RaceMilkType value)
    {
        comp.milkDef = DefDatabase<ThingDef>.GetNamed(value.milkTypeDefName, true);
        comp.milkIntervalDays = 1;
        comp.milkFemaleOnly = false;
    }

    public static CompEquallyMilkable CompEquallyMilkable(this ThingWithComps thing)
    {
        if (thing is not Pawn pawn) { return null; }
        CompEquallyMilkable comp = pawn.TryGetComp<CompEquallyMilkable>();
        if (comp == null)
        {
            comp = new CompEquallyMilkable
            {
                parent = pawn,
                props = pawn.def.GetCompProperties<CompProperties_Milkable>() ??
                    new CompProperties_Milkable()
                    {
                        milkDef = pawn.MilkDef() ?? DefDatabase<ThingDef>.GetNamed("Milk", true),
                        milkAmount = (int)pawn.MilkAmount(),
                        milkIntervalDays = 1,
                        milkFemaleOnly = false
                    }
            };
            pawn.AllComps.Add(comp);
        }
        return comp;
    }
}
