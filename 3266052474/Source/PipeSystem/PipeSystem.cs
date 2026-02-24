using HarmonyLib;
using Verse;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using System.Linq;
using EqualMilking.Helpers;
using PipeSystem;
namespace EqualMilking.PipeSystem;
[StaticConstructorOnStartup]
internal static class ApplyPatches
{
    private static readonly Harmony Harmony;
    public static Dictionary<ThingDef, List<PipeNetDef>> pipeNets = new();
    private static string Pipe => "EM_Pipe".Translate();
    private static string Tap => "EM_Tap".Translate();
    private static string Hidden => "EM_Hidden".Translate();
    private static string Valve => "EM_Valve".Translate();


    static ApplyPatches()
    {
        Harmony = new Harmony("com.akaster.rimworld.mod.equalmilking.pipe");
        Log.Message("[Equal Milking]: Vanilla Expanded Framework Loaded, Adding Pipe Systems...");
        AddResourceConversions();
        EventHelper.OnPostLoadLong += RegisterPipeNets;
        EventHelper.OnPostLoadLong += ResolveLang;
        EventHelper.OnPostLoadGame += AdjustContainerSize;
        EventHelper.OnPostNewGame += AdjustContainerSize;
        Harmony.PatchAll();
    }
    public static void ResolveLang()
    {
        DesignationCategoryDef def = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("EM_PipeNetworks");
        if (def != null)
        {
            def.label = Lang.Join(Lang.Milk, Pipe);
        }
        DefDatabase<DesignatorDropdownGroupDef>.GetNamed("EM_MilkTaps").label = Lang.Join(Lang.Milk, Tap);
        DefDatabase<DesignatorDropdownGroupDef>.GetNamed("EM_MilkPipes").label = Lang.Join(Lang.Milk, Pipe);

        DefDatabase<ThingDef>.GetNamed("EM_MilkTap").label = Lang.Join(Lang.Milk, Tap);
        DefDatabase<ThingDef>.GetNamed("EM_HumanMilkTap").label = Lang.Join(Lang.Human, Lang.Milk, Tap);
        DefDatabase<ThingDef>.GetNamed("EM_MilkPipe").label = Lang.Join(Lang.Milk, Pipe);
        DefDatabase<ThingDef>.GetNamed("EM_UndergroundMilkPipe").label = Lang.Join(Hidden, Lang.Milk, Pipe);
        DefDatabase<ThingDef>.GetNamed("EM_MilkValve").label = Lang.Join(Lang.Milk, Valve);
        DefDatabase<ThingDef>.GetNamed("EM_MilkContainer").label = Lang.Join(Lang.Milk, Lang.Container);

        DefDatabase<PipeNetDef>.GetNamed("EM_MilkNet").resource.name = Lang.Milk;
        DefDatabase<PipeNetDef>.GetNamed("EM_HumanMilkNet").resource.name = Lang.Join(Lang.Human, Lang.Milk);
    }
    public static void RegisterPipeNets()
    {
        List<ThingDef> milkingBuilding = DefDatabase<ThingDef>.AllDefs.Where(x => x.thingClass == typeof(Building_Milking)).Except(EMDefOf.EM_MilkingSpot).ToList();
        foreach (PipeNetDef def in DefDatabase<PipeNetDef>.AllDefs)
        {
            foreach (ThingDef thingDef in milkingBuilding)
            {
                if (def.defName == "EM_MilkNet" || def.defName == "EM_HumanMilkNet") { continue; }
                thingDef.comps.Add(new CompProperties_Resource { pipeNet = def });
            }
        }
        foreach (CompProperties_ConvertThingToResource compProperties_ConvertThingToResource in DefDatabase<ThingDef>.AllDefs.SelectMany(x => x.comps).Where(x => x is CompProperties_ConvertThingToResource).Cast<CompProperties_ConvertThingToResource>())
        {
            if (!pipeNets.ContainsKey(compProperties_ConvertThingToResource.thing)) { pipeNets[compProperties_ConvertThingToResource.thing] = new List<PipeNetDef>(); }
            pipeNets[compProperties_ConvertThingToResource.thing].AddDistinct(compProperties_ConvertThingToResource.pipeNet);
        }
        //Some only has convert to thing, like nutrient paste expanded
        foreach (CompProperties_ConvertResourceToThing compProperties_ConvertResourceToThing in DefDatabase<ThingDef>.AllDefs.SelectMany(x => x.comps).Where(x => x is CompProperties_ConvertResourceToThing).Cast<CompProperties_ConvertResourceToThing>())
        {
            if (!pipeNets.ContainsKey(compProperties_ConvertResourceToThing.thing)) { pipeNets[compProperties_ConvertResourceToThing.thing] = new List<PipeNetDef>(); }
            pipeNets[compProperties_ConvertResourceToThing.thing].AddDistinct(compProperties_ConvertResourceToThing.pipeNet);
        }
    }
    public static void AdjustContainerSize()
    {
        // Adjust milk container capacity dynamically
        foreach (CompProperties_ResourceStorage comp in DefDatabase<ThingDef>.GetNamed("EM_MilkContainer").comps.Where(x => x is CompProperties_ResourceStorage).Cast<CompProperties_ResourceStorage>())
        {
            foreach (KeyValuePair<ThingDef, List<PipeNetDef>> pair in pipeNets)
            {
                if (pair.Value.Contains(comp.pipeNet))
                {
                    comp.storageCapacity = Mathf.Max(comp.storageCapacity, pair.Key.stackLimit * 10);
                    break;
                }
            }
        }
    }
    public static void AddResourceConversions()
    {
        ThingDef milkDef = DefDatabase<ThingDef>.GetNamed("Milk");
        EMDefOf.EM_MilkingPump.comps.Add(new CompProperties_ConvertThingToResource { thing = milkDef, pipeNet = DefDatabase<PipeNetDef>.GetNamed("EM_MilkNet") });
        EMDefOf.EM_MilkingPump.comps.Add(new CompProperties_ConvertThingToResource { thing = EMDefOf.EM_HumanMilk, pipeNet = DefDatabase<PipeNetDef>.GetNamed("EM_HumanMilkNet") });
        EMDefOf.EM_MilkingElectric.comps.Add(new CompProperties_ConvertThingToResource { thing = milkDef, pipeNet = DefDatabase<PipeNetDef>.GetNamed("EM_MilkNet") });
        EMDefOf.EM_MilkingElectric.comps.Add(new CompProperties_ConvertThingToResource { thing = EMDefOf.EM_HumanMilk, pipeNet = DefDatabase<PipeNetDef>.GetNamed("EM_HumanMilkNet") });
    }
    public static bool IsConnectedToPipeNetStorage(this ThingWithComps thing, PipeNetDef def)
    {
        foreach (CompResource compResource in thing.GetComps<CompResource>())
        {
            if (compResource.PipeNet != null && compResource.PipeNet.def == def)
            {
                return compResource.PipeNet.storages.Count > 0;
            }
        }
        return false;
    }
    public static PipeNet GetPipeNetForThing(this ThingWithComps thing, ThingDef thingDef)
    {
        if (!pipeNets.ContainsKey(thingDef)) { return null; }
        return thing.GetComps<CompResource>().FirstOrDefault(x => x.PipeNet?.AvailableCapacity > 0 && pipeNets[thingDef]?.Contains(x.PipeNet.def) == true)?.PipeNet;
    }
    [HarmonyPatch(typeof(Building_Milking), nameof(Building_Milking.PlaceMilkThing))]
    public static class Building_Milking_PlaceMilkThing_Patch
    {
        public static bool Prefix(Building_Milking __instance, ref Thing milkThing)
        {
            if (__instance.GetPipeNetForThing(milkThing.def) is PipeNet pipeNet)
            {
                pipeNet.DistributeAmongStorage(milkThing.stackCount, out float stored);
                milkThing.stackCount -= (int)stored;
                if (milkThing.stackCount <= 0) { milkThing.Destroy(DestroyMode.Vanish); return false; }
            }
            return true;
        }
    }
    /// <summary>
    /// Actually draw the damn attachments
    /// </summary>
    [HarmonyPatch(typeof(ThingWithComps), "DrawAt")]
    public static class ThingWithComps_DrawAt_Patch
    {
        public static void Postfix(ThingWithComps __instance, Vector3 drawLoc, bool flip)
        {
            if (__instance.def.modContentPack?.Name == EqualMilkingMod.equalMilkingMod.Name && __instance.def.graphicData?.attachments != null)
            {
                foreach (GraphicData attachment in __instance.def.graphicData.attachments)
                {
                    attachment.Graphic.Draw(drawLoc, flip ? __instance.Rotation.Opposite : __instance.Rotation, __instance);
                }
            }
        }
    }
}
public class CompMultiResourceStorage : CompResourceStorage
{
    public override void PostExposeData()
    {
        if (this.amountStored > this.Props.storageCapacity)
        {
            this.amountStored = this.Props.storageCapacity;
        }
        Scribe_Values.Look(ref this.amountStored, "storedResource_" + this.Props.Resource.name.Replace(" ", "_"), 0f, false);
        Scribe_Values.Look(ref this.ticksWithoutPower, "tickWithoutPower", 0, false);
        Scribe_Values.Look(ref this.markedForExtract, "markedForExtract", false, false);
        Scribe_Values.Look(ref this.markedForTransfer, "markedForTransfer", false, false);
        Scribe_Values.Look(ref this.markedForRefill, "markedForRefill", false, false);
    }
}
public class CompProperties_MultiResourceStorage : CompProperties_ResourceStorage
{
    public CompProperties_MultiResourceStorage()
    {
        this.compClass = typeof(CompMultiResourceStorage);
    }
}
[HarmonyPatch(typeof(Alert_NoStorage))]
public static class Alert_NoStorage_Patch
{
    /// <summary>
    /// Remove no storage alert for milking buildings
    /// </summary>
    /// <param name="__result"></param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Alert_NoStorage.ThingsList))]
    public static void ThingsList_Postfix(ref List<Thing> __result)
    {
        __result.RemoveAll(x => x is Building_Milking);
    }
}
