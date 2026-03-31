using HarmonyLib;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Cum.Common;
using rjw;
using Verse;

namespace MilkCum.Fluids.Cum.SemenPool.Patches;

[HarmonyPatch(typeof(SexUtility), nameof(SexUtility.TransferFluids))]
[HarmonyPriority(Priority.First)]
public static class Patch_TransferFluids_VirtualSemenLedgerBegin
{
    public static void Prefix() => VirtualSemenRecordLedger.BeginTransfer();
}

[HarmonyPatch(typeof(SexUtility), nameof(SexUtility.TransferFluids))]
[HarmonyPriority(Priority.Last)]
public static class Patch_TransferFluids_VirtualSemenLedgerCleanup
{
    public static void Postfix() => VirtualSemenRecordLedger.ClearRemaining();
}

[HarmonyPatch(typeof(SexUtility), nameof(SexUtility.IngestFluids))]
public static class Patch_IngestFluids_VirtualSemenPool
{
    public static void Prefix(Pawn receiver, Pawn giver, ISexPartHediff fromPart, ISexPartHediff toPart, ref float totalNutritionLost)
    {
        _ = receiver;
        _ = toPart;
        _ = totalNutritionLost;
        if (!MilkCumSettings.Cum_EnableVirtualSemenPool || giver == null || fromPart == null) return;
        HediffComp_SexPart comp = fromPart.GetPartComp();
        if (comp == null) return;
        float nominal = comp.GetFluidAmount();
        float actual = giver.CompVirtualSemenPool().ConsumeForEjaculation(fromPart, nominal, registerForFluidRecords: true);
        SemenPoolFluidAmountGate.Enter(comp, actual);
    }

    public static void Postfix() => SemenPoolFluidAmountGate.Exit();
}

[HarmonyPatch(typeof(HediffComp_SexPart), nameof(HediffComp_SexPart.GetFluidAmount))]
public static class Patch_HediffComp_SexPart_GetFluidAmount_VirtualSemenGate
{
    public static void Postfix(HediffComp_SexPart __instance, ref float __result)
    {
        if (SemenPoolFluidAmountGate.TryGetOverride(__instance, out float v))
            __result = v;
    }
}
