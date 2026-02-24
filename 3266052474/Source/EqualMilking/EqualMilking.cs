using Verse;
using RimWorld;
using EqualMilking.Helpers;
namespace EqualMilking;
[StaticConstructorOnStartup]
public static class EqualMilking
{
    static EqualMilking()
    {
        LongEventHandler.QueueLongEvent(() => { EventHelper.TriggerPostLoadLong(); }, "EqualMilking_LongEvent", false, null);

        EventHelper.OnPostLoadLong += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnPostNewGame += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnPostLoadGame += EqualMilkingMod.Settings.UpdateEqualMilkingSettings;
        EventHelper.OnSettingsChanged += GeneHelper.ReloadImpliedGenes;
        EventHelper.OnPostLoadLong += Init;
        // Patch vanilla
        EqualMilkingMod.Harmony.PatchAll();
    }
    public static void Init()
    {
        JobDefOf.Milk.driverClass = typeof(JobDriver_EquallyMilk);
        DefDatabase<WorkGiverDef>.GetNamed("Milk").giverClass = typeof(WorkGiver_EquallyMilk);
        HediffDefOf.Lactating.hediffClass = typeof(HediffWithComps_EqualMilkingLactating);
        StatCategoryDefOf.AnimalProductivity.displayAllByDefault = true;

        // label auto translations
        if (DefDatabase<ThingDef>.GetNamedSilentFail("VCE_Cheese") is ThingDef cheese)
        {
            ThingDef humanMilkCheese = DefDatabase<ThingDef>.GetNamed("VCE_HumanMilkCheese");
            humanMilkCheese.label = Lang.Join(Lang.Human, Lang.Milk, cheese.label);
            humanMilkCheese.description = cheese.description;
        }
    }
}
